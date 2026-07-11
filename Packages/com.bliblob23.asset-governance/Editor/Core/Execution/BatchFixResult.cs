using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 汇总一次批量修复中成功、失败和跳过的问题。
    /// </summary>
    public sealed class BatchFixResult
    {
        internal BatchFixResult(
            IEnumerable<FixResult> fixResults,
            IEnumerable<ValidationIssue> skippedIssues)
        {
            if (fixResults == null)
            {
                throw new ArgumentNullException(nameof(fixResults));
            }

            if (skippedIssues == null)
            {
                throw new ArgumentNullException(nameof(skippedIssues));
            }

            FixResults = new ReadOnlyCollection<FixResult>(fixResults.ToList());
            SkippedIssues = new ReadOnlyCollection<ValidationIssue>(skippedIssues.ToList());
        }

        public IReadOnlyList<FixResult> FixResults { get; }

        public IReadOnlyList<ValidationIssue> SkippedIssues { get; }

        public int SucceededCount => FixResults.Count(result => result.Succeeded);

        public int FailedCount => FixResults.Count - SucceededCount;

        public int SkippedCount => SkippedIssues.Count;
    }
}
