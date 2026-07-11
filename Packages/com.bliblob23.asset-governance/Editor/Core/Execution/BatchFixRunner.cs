using System;
using System.Collections.Generic;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 逐条刷新资源上下文并执行批量修复，确保单条失败不会中止后续问题。
    /// </summary>
    public static class BatchFixRunner
    {
        /// <summary>
        /// 使用自动发现的规则修复指定问题。
        /// </summary>
        public static BatchFixResult Fix(IEnumerable<ValidationIssue> issues)
        {
            return Fix(
                issues,
                assetPath => AssetScanner.Scan(new[] { assetPath }),
                RuleRegistry.DiscoverRules());
        }

        internal static BatchFixResult Fix(
            IEnumerable<ValidationIssue> issues,
            Func<string, IReadOnlyList<AssetContext>> scanAsset,
            IEnumerable<IAssetRule> rules)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            if (scanAsset == null)
            {
                throw new ArgumentNullException(nameof(scanAsset));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var issueList = new List<ValidationIssue>();
            foreach (var issue in issues)
            {
                if (issue == null)
                {
                    throw new ArgumentException(
                        "Issue collections cannot contain null.",
                        nameof(issues));
                }

                issueList.Add(issue);
            }

            var ruleList = new List<IAssetRule>(rules);
            var fixResults = new List<FixResult>();
            var skippedIssues = new List<ValidationIssue>();

            foreach (var issue in issueList)
            {
                if (!issue.CanFix)
                {
                    skippedIssues.Add(issue);
                    continue;
                }

                IReadOnlyList<AssetContext> contexts;
                try
                {
                    contexts = scanAsset(issue.AssetPath);
                }
                catch (Exception exception)
                {
                    fixResults.Add(CreateFailure(issue, exception));
                    continue;
                }

                if (contexts == null)
                {
                    fixResults.Add(CreateFailure(
                        issue,
                        new InvalidOperationException(
                            $"Scanning asset '{issue.AssetPath}' returned no result collection.")));
                    continue;
                }

                if (contexts.Count == 0)
                {
                    skippedIssues.Add(issue);
                    continue;
                }

                fixResults.Add(FixRunner.Fix(contexts[0], issue, ruleList));
            }

            return new BatchFixResult(fixResults, skippedIssues);
        }

        private static FixResult CreateFailure(
            ValidationIssue issue,
            Exception exception)
        {
            return new FixResult(
                issue,
                new RuleExecutionError(
                    issue.RuleId,
                    issue.AssetPath,
                    RuleExecutionStage.Fix,
                    exception));
        }
    }
}
