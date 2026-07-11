using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 调度资源规则、隔离单条规则异常并汇总检查结果。
    /// </summary>
    public static class RuleRunner
    {
        /// <summary>
        /// 使用自动发现的全部规则检查指定资源。
        /// </summary>
        public static ValidationRunResult Run(IEnumerable<AssetContext> assetContexts)
        {
            return Run(assetContexts, RuleRegistry.DiscoverRules());
        }

        /// <summary>
        /// 使用指定规则检查指定资源，便于执行规则子集和独立测试。
        /// </summary>
        public static ValidationRunResult Run(
            IEnumerable<AssetContext> assetContexts,
            IEnumerable<IAssetRule> rules)
        {
            var contextSnapshot = CreateContextSnapshot(assetContexts);
            var ruleSnapshot = CreateRuleSnapshot(rules);
            var issues = new List<ValidationIssue>();
            var executionErrors = new List<RuleExecutionError>();

            foreach (var context in contextSnapshot)
            {
                foreach (var rule in ruleSnapshot)
                {
                    ExecuteRule(rule, context, issues, executionErrors);
                }
            }

            issues.Sort(CompareIssues);
            executionErrors.Sort(CompareExecutionErrors);

            return new ValidationRunResult(
                new ReadOnlyCollection<ValidationIssue>(issues),
                new ReadOnlyCollection<RuleExecutionError>(executionErrors));
        }

        private static List<AssetContext> CreateContextSnapshot(
            IEnumerable<AssetContext> assetContexts)
        {
            if (assetContexts == null)
            {
                throw new ArgumentNullException(nameof(assetContexts));
            }

            var contexts = new List<AssetContext>();
            foreach (var context in assetContexts)
            {
                if (context == null)
                {
                    throw new ArgumentException(
                        "Asset context collections cannot contain null.",
                        nameof(assetContexts));
                }

                contexts.Add(context);
            }

            contexts.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.AssetPath, right.AssetPath));
            return contexts;
        }

        private static List<PreparedRule> CreateRuleSnapshot(IEnumerable<IAssetRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var preparedRules = new List<PreparedRule>();
            foreach (var rule in rules)
            {
                if (rule == null)
                {
                    throw new ArgumentException(
                        "Rule collections cannot contain null.",
                        nameof(rules));
                }

                var descriptor = rule.Descriptor;
                if (descriptor == null)
                {
                    throw new InvalidOperationException(
                        $"Rule type '{rule.GetType().FullName}' returned a null descriptor.");
                }

                preparedRules.Add(new PreparedRule(rule, descriptor));
            }

            preparedRules.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.Descriptor.Id, right.Descriptor.Id));
            return preparedRules;
        }

        private static void ExecuteRule(
            PreparedRule preparedRule,
            AssetContext context,
            ICollection<ValidationIssue> issues,
            ICollection<RuleExecutionError> executionErrors)
        {
            if (!IsRuleEnabled(preparedRule, context, executionErrors))
            {
                return;
            }

            bool canEvaluate;
            try
            {
                canEvaluate = preparedRule.Rule.CanEvaluate(context);
            }
            catch (Exception exception)
            {
                executionErrors.Add(new RuleExecutionError(
                    preparedRule.Descriptor.Id,
                    context.AssetPath,
                    RuleExecutionStage.CanEvaluate,
                    exception));
                return;
            }

            if (!canEvaluate)
            {
                return;
            }

            IEnumerable<ValidationIssue> ruleIssues;
            try
            {
                ruleIssues = preparedRule.Rule.Evaluate(context);
            }
            catch (Exception exception)
            {
                AddEvaluationError(preparedRule, context, exception, executionErrors);
                return;
            }

            if (ruleIssues == null)
            {
                AddEvaluationError(
                    preparedRule,
                    context,
                    new InvalidOperationException("Rules cannot return a null issue collection."),
                    executionErrors);
                return;
            }

            var severityResolved = false;
            var hasSeverityOverride = false;
            var severityOverride = default(RuleSeverity);

            try
            {
                foreach (var issue in ruleIssues)
                {
                    if (issue == null)
                    {
                        AddEvaluationError(
                            preparedRule,
                            context,
                            new InvalidOperationException("Rule issue collections cannot contain null."),
                            executionErrors);
                        continue;
                    }

                    if (!severityResolved)
                    {
                        hasSeverityOverride = TryGetSeverityOverride(
                            preparedRule,
                            context,
                            executionErrors,
                            out severityOverride);
                        severityResolved = true;
                    }

                    issues.Add(hasSeverityOverride
                        ? CopyIssueWithSeverity(issue, severityOverride)
                        : issue);
                }
            }
            catch (Exception exception)
            {
                AddEvaluationError(preparedRule, context, exception, executionErrors);
            }
        }

        private static bool IsRuleEnabled(
            PreparedRule preparedRule,
            AssetContext context,
            ICollection<RuleExecutionError> executionErrors)
        {
            if (context.GovernanceProfile == null)
            {
                return true;
            }

            try
            {
                return context.GovernanceProfile.IsRuleEnabled(preparedRule.Descriptor.Id);
            }
            catch (Exception exception)
            {
                executionErrors.Add(new RuleExecutionError(
                    preparedRule.Descriptor.Id,
                    context.AssetPath,
                    RuleExecutionStage.Configuration,
                    exception));
                return false;
            }
        }

        private static bool TryGetSeverityOverride(
            PreparedRule preparedRule,
            AssetContext context,
            ICollection<RuleExecutionError> executionErrors,
            out RuleSeverity severity)
        {
            if (context.GovernanceProfile == null)
            {
                severity = default;
                return false;
            }

            try
            {
                return context.GovernanceProfile.TryGetSeverityOverride(
                    preparedRule.Descriptor.Id,
                    out severity);
            }
            catch (Exception exception)
            {
                executionErrors.Add(new RuleExecutionError(
                    preparedRule.Descriptor.Id,
                    context.AssetPath,
                    RuleExecutionStage.Configuration,
                    exception));
                severity = default;
                return false;
            }
        }

        private static ValidationIssue CopyIssueWithSeverity(
            ValidationIssue issue,
            RuleSeverity severity)
        {
            if (issue.Severity == severity)
            {
                return issue;
            }

            return new ValidationIssue(
                issue.RuleId,
                severity,
                issue.AssetPath,
                issue.Message);
        }

        private static void AddEvaluationError(
            PreparedRule preparedRule,
            AssetContext context,
            Exception exception,
            ICollection<RuleExecutionError> executionErrors)
        {
            executionErrors.Add(new RuleExecutionError(
                preparedRule.Descriptor.Id,
                context.AssetPath,
                RuleExecutionStage.Evaluate,
                exception));
        }

        private static int CompareIssues(ValidationIssue left, ValidationIssue right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.AssetPath, right.AssetPath);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.Ordinal.Compare(left.RuleId, right.RuleId);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Severity.CompareTo(right.Severity);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(left.Message, right.Message);
        }

        private static int CompareExecutionErrors(
            RuleExecutionError left,
            RuleExecutionError right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.AssetPath, right.AssetPath);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.Ordinal.Compare(left.RuleId, right.RuleId);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Stage.CompareTo(right.Stage);
            if (comparison != 0)
            {
                return comparison;
            }

            return StringComparer.Ordinal.Compare(
                left.Exception.Message,
                right.Exception.Message);
        }

        private sealed class PreparedRule
        {
            public PreparedRule(IAssetRule rule, RuleDescriptor descriptor)
            {
                Rule = rule;
                Descriptor = descriptor;
            }

            public IAssetRule Rule { get; }

            public RuleDescriptor Descriptor { get; }
        }
    }
}
