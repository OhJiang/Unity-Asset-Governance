using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 保存项目级资源治理配置和各规则的强类型设置引用。
    /// </summary>
    [CreateAssetMenu(
        fileName = "AssetGovernanceProfile",
        menuName = "Asset Governance/Governance Profile")]
    public sealed class GovernanceProfile : ScriptableObject
    {
        [SerializeField]
        private List<string> excludedPaths = new List<string>();

        [SerializeField]
        private List<RuleState> ruleStates = new List<RuleState>();

        [SerializeField]
        private List<AssetRuleSettings> ruleSettings = new List<AssetRuleSettings>();

        /// <summary>
        /// 获取当前 Profile 中的全局排除路径只读视图。
        /// </summary>
        public IReadOnlyList<string> ExcludedPaths =>
            new ReadOnlyCollection<string>(excludedPaths);

        /// <summary>
        /// 获取当前 Profile 中的规则启用状态只读视图。
        /// </summary>
        public IReadOnlyList<RuleState> RuleStates =>
            new ReadOnlyCollection<RuleState>(ruleStates);

        /// <summary>
        /// 获取当前 Profile 引用的规则配置只读视图。
        /// </summary>
        public IReadOnlyList<AssetRuleSettings> RuleSettings =>
            new ReadOnlyCollection<AssetRuleSettings>(ruleSettings);

        /// <summary>
        /// 获取指定资源是否被项目级路径配置排除。
        /// 排除项同时匹配自身和其下级资源，但不会匹配名称相似的相邻路径。
        /// </summary>
        public bool IsAssetPathExcluded(string assetPath)
        {
            var normalizedAssetPath = NormalizeAssetPath(assetPath, nameof(assetPath));
            var isExcluded = false;

            foreach (var excludedPath in excludedPaths)
            {
                if (string.IsNullOrWhiteSpace(excludedPath))
                {
                    throw new InvalidOperationException(
                        $"Governance profile '{name}' contains an empty excluded path.");
                }

                var normalizedExcludedPath = NormalizeAssetPath(excludedPath, null);
                if (!IsProjectAssetPath(normalizedExcludedPath))
                {
                    throw new InvalidOperationException(
                        $"Governance profile '{name}' contains an invalid excluded path " +
                        $"'{excludedPath}'. Paths must start with 'Assets' or 'Packages'.");
                }

                if (string.Equals(
                        normalizedAssetPath,
                        normalizedExcludedPath,
                        StringComparison.Ordinal) ||
                    normalizedAssetPath.StartsWith(
                        normalizedExcludedPath + "/",
                        StringComparison.Ordinal))
                {
                    isExcluded = true;
                }
            }

            return isExcluded;
        }

        private static string NormalizeAssetPath(string path, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                if (parameterName != null)
                {
                    throw new ArgumentException("An asset path is required.", parameterName);
                }

                return string.Empty;
            }

            var normalizedPath = path.Trim().Replace('\\', '/').TrimEnd('/');
            if (!IsProjectAssetPath(normalizedPath) && parameterName != null)
            {
                throw new ArgumentException(
                    "Asset paths must start with 'Assets' or 'Packages'.",
                    parameterName);
            }

            return normalizedPath;
        }

        private static bool IsProjectAssetPath(string path)
        {
            return string.Equals(path, "Assets", StringComparison.Ordinal) ||
                   path.StartsWith("Assets/", StringComparison.Ordinal) ||
                   string.Equals(path, "Packages", StringComparison.Ordinal) ||
                   path.StartsWith("Packages/", StringComparison.Ordinal);
        }

        /// <summary>
        /// 获取指定规则是否启用。未配置的规则默认启用。
        /// 空规则 ID、空状态条目和重复规则 ID 会产生明确异常。
        /// </summary>
        public bool IsRuleEnabled(string ruleId)
        {
            var matchedState = FindRuleState(ruleId);
            return matchedState == null || matchedState.Enabled;
        }

        /// <summary>
        /// 尝试获取指定规则的项目严重级别覆盖。未配置或未启用覆盖时返回 false。
        /// </summary>
        public bool TryGetSeverityOverride(
            string ruleId,
            out RuleSeverity severity)
        {
            var matchedState = FindRuleState(ruleId);
            if (matchedState == null || !matchedState.OverrideSeverity)
            {
                severity = default;
                return false;
            }

            if (!Enum.IsDefined(typeof(RuleSeverity), matchedState.Severity))
            {
                throw new InvalidOperationException(
                    $"Governance profile '{name}' contains an invalid severity for rule '{ruleId}'.");
            }

            severity = matchedState.Severity;
            return true;
        }

        private RuleState FindRuleState(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException("A rule ID is required.", nameof(ruleId));
            }

            RuleState matchedState = null;

            foreach (var candidate in ruleStates)
            {
                if (candidate == null)
                {
                    throw new InvalidOperationException(
                        $"Governance profile '{name}' contains a missing rule state.");
                }

                if (string.IsNullOrWhiteSpace(candidate.RuleId))
                {
                    throw new InvalidOperationException(
                        $"Governance profile '{name}' contains a rule state with an empty rule ID.");
                }

                if (!string.Equals(candidate.RuleId, ruleId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (matchedState != null)
                {
                    throw new InvalidOperationException(
                        $"Governance profile '{name}' contains duplicate states for rule '{ruleId}'.");
                }

                matchedState = candidate;
            }

            return matchedState;
        }

        /// <summary>
        /// 尝试按规则 ID 获取指定强类型配置。
        /// 未配置时返回 false；重复 ID、空 ID 或类型不匹配时抛出明确异常。
        /// </summary>
        public bool TryGetRuleSettings<TSettings>(
            string ruleId,
            out TSettings settings)
            where TSettings : AssetRuleSettings
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException("A rule ID is required.", nameof(ruleId));
            }

            AssetRuleSettings matchedSettings = null;

            foreach (var candidate in ruleSettings)
            {
                if (candidate == null)
                {
                    throw new InvalidOperationException(
                        $"Governance profile '{name}' contains a missing rule settings reference.");
                }

                if (string.IsNullOrWhiteSpace(candidate.RuleId))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{candidate.name}' has an empty rule ID.");
                }

                if (!string.Equals(candidate.RuleId, ruleId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (matchedSettings != null)
                {
                    throw new InvalidOperationException(
                        $"Governance profile '{name}' contains duplicate settings for rule '{ruleId}'.");
                }

                matchedSettings = candidate;
            }

            if (matchedSettings == null)
            {
                settings = null;
                return false;
            }

            settings = matchedSettings as TSettings;
            if (settings == null)
            {
                throw new InvalidOperationException(
                    $"Settings for rule '{ruleId}' must be of type '{typeof(TSettings).FullName}', " +
                    $"but found '{matchedSettings.GetType().FullName}'.");
            }

            return true;
        }
    }
}
