using System;
using System.Collections.Generic;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 定位可修复规则、执行单条修复并将异常转换为结构化结果。
    /// </summary>
    public static class FixRunner
    {
        /// <summary>
        /// 使用自动发现的规则修复指定问题。
        /// </summary>
        public static FixResult Fix(AssetContext context, ValidationIssue issue)
        {
            return Fix(context, issue, RuleRegistry.DiscoverRules());
        }

        /// <summary>
        /// 使用显式规则集合修复指定问题，便于工具入口和独立测试。
        /// </summary>
        public static FixResult Fix(
            AssetContext context,
            ValidationIssue issue,
            IEnumerable<IAssetRule> rules)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (!string.Equals(context.AssetPath, issue.AssetPath, StringComparison.Ordinal))
            {
                return Failure(
                    issue,
                    context.AssetPath,
                    RuleExecutionStage.Fix,
                    new InvalidOperationException(
                        "The issue asset path does not match the provided asset context."));
            }

            IAssetRule matchedRule = null;
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

                if (!string.Equals(descriptor.Id, issue.RuleId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (matchedRule != null)
                {
                    return Failure(
                        issue,
                        context.AssetPath,
                        RuleExecutionStage.Fix,
                        new InvalidOperationException(
                            $"Multiple rules were provided for issue rule ID '{issue.RuleId}'."));
                }

                matchedRule = rule;
            }

            if (!(matchedRule is IFixableAssetRule fixableRule))
            {
                return Failure(
                    issue,
                    context.AssetPath,
                    RuleExecutionStage.Fix,
                    new InvalidOperationException(
                        $"Rule '{issue.RuleId}' does not provide an automatic fix."));
            }

            bool canFix;
            try
            {
                canFix = fixableRule.CanFix(context, issue);
            }
            catch (Exception exception)
            {
                return Failure(
                    issue,
                    context.AssetPath,
                    RuleExecutionStage.CanFix,
                    exception);
            }

            if (!canFix)
            {
                return Failure(
                    issue,
                    context.AssetPath,
                    RuleExecutionStage.CanFix,
                    new InvalidOperationException(
                        $"Rule '{issue.RuleId}' cannot fix the issue in its current state."));
            }

            try
            {
                fixableRule.Fix(context, issue);
            }
            catch (Exception exception)
            {
                return Failure(
                    issue,
                    context.AssetPath,
                    RuleExecutionStage.Fix,
                    exception);
            }

            return new FixResult(issue, null);
        }

        private static FixResult Failure(
            ValidationIssue issue,
            string assetPath,
            RuleExecutionStage stage,
            Exception exception)
        {
            return new FixResult(
                issue,
                new RuleExecutionError(issue.RuleId, assetPath, stage, exception));
        }
    }
}
