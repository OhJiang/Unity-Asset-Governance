using System;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 描述规则在检查资源时产生的框架执行错误。
    /// 它与资源本身违反规则产生的 <see cref="ValidationIssue"/> 分开保存。
    /// </summary>
    public sealed class RuleExecutionError
    {
        public RuleExecutionError(
            string ruleId,
            string assetPath,
            RuleExecutionStage stage,
            Exception exception)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException("A rule ID is required.", nameof(ruleId));
            }

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("An asset path is required.", nameof(assetPath));
            }

            if (!Enum.IsDefined(typeof(RuleExecutionStage), stage))
            {
                throw new ArgumentOutOfRangeException(nameof(stage));
            }

            RuleId = ruleId;
            AssetPath = assetPath;
            Stage = stage;
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public string RuleId { get; }

        public string AssetPath { get; }

        public RuleExecutionStage Stage { get; }

        public Exception Exception { get; }
    }
}
