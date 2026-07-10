using System.Collections.Generic;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 保存一次规则执行产生的资源问题和框架执行错误。
    /// </summary>
    public sealed class ValidationRunResult
    {
        internal ValidationRunResult(
            IReadOnlyList<ValidationIssue> issues,
            IReadOnlyList<RuleExecutionError> executionErrors)
        {
            Issues = issues;
            ExecutionErrors = executionErrors;
        }

        public IReadOnlyList<ValidationIssue> Issues { get; }

        public IReadOnlyList<RuleExecutionError> ExecutionErrors { get; }

        public bool HasExecutionErrors => ExecutionErrors.Count > 0;
    }
}
