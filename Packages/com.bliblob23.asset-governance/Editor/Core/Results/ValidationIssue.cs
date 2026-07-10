using System;

namespace UnityAssetGovernance
{
    /// <summary>
    /// Immutable issue reported by an asset rule.
    /// </summary>
    public sealed class ValidationIssue
    {
        public ValidationIssue(
            string ruleId,
            RuleSeverity severity,
            string assetPath,
            string message)
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
        }

        public string RuleId { get; }

        public RuleSeverity Severity { get; }

        public string AssetPath { get; }

        public string Message { get; }
    }
}
