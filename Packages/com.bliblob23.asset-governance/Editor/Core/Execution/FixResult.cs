using System;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 描述一次显式资源修复的执行结果。
    /// </summary>
    public sealed class FixResult
    {
        internal FixResult(
            ValidationIssue issue,
            RuleExecutionError executionError)
        {
            Issue = issue ?? throw new ArgumentNullException(nameof(issue));
            ExecutionError = executionError;
        }

        public ValidationIssue Issue { get; }

        public bool Succeeded => ExecutionError == null;

        public RuleExecutionError ExecutionError { get; }
    }
}
