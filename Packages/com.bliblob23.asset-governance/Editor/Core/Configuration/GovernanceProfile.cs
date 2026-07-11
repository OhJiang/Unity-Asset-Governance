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
        private List<AssetRuleSettings> ruleSettings = new List<AssetRuleSettings>();

        /// <summary>
        /// 获取当前 Profile 引用的规则配置只读视图。
        /// </summary>
        public IReadOnlyList<AssetRuleSettings> RuleSettings =>
            new ReadOnlyCollection<AssetRuleSettings>(ruleSettings);

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
