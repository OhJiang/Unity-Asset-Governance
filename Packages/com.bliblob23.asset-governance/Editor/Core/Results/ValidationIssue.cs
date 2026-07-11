using System;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 资源规则报告的不可变验证问题。
    /// </summary>
    public sealed class ValidationIssue
    {
        public ValidationIssue(
            string ruleId,
            RuleSeverity severity,
            string assetPath,
            string message)
            : this(ruleId, severity, assetPath, message, false)
        {
        }

        internal ValidationIssue(
            string ruleId,
            RuleSeverity severity,
            string assetPath,
            string message,
            bool canFix)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException("A rule ID is required.", nameof(ruleId));
            }

            if (!Enum.IsDefined(typeof(RuleSeverity), severity))
            {
                throw new ArgumentOutOfRangeException(nameof(severity));
            }

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("An asset path is required.", nameof(assetPath));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("An issue message is required.", nameof(message));
            }

            RuleId = ruleId;
            Severity = severity;
            AssetPath = assetPath;
            Message = message;
            CanFix = canFix;
        }

        public string RuleId { get; }

        public RuleSeverity Severity { get; }

        public string AssetPath { get; }

        public string Message { get; }

        /// <summary>
        /// 获取框架是否已确认当前问题可以通过对应规则自动修复。
        /// </summary>
        public bool CanFix { get; }
    }
}
