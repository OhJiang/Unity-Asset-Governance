using System;
using UnityEngine;

namespace UnityAssetGovernance.CustomRuleExample
{
    /// <summary>
    /// 示例规则自己的强类型配置。框架核心只依赖 AssetRuleSettings，
    /// 因此新增此类型不需要修改 GovernanceProfile。
    /// </summary>
    [CreateAssetMenu(
        fileName = "TextureNamePrefixRuleSettings",
        menuName = "Asset Governance/Samples/Texture Name Prefix Rule")]
    public sealed class TextureNamePrefixRuleSettings : AssetRuleSettings
    {
        public const string StableRuleId = "SAMPLE-TEX-001";

        [SerializeField]
        private string assetPathPrefix = "Assets/Art/Textures";

        [SerializeField]
        private string requiredNamePrefix = "T_";

        public override string RuleId => StableRuleId;

        public string RequiredNamePrefix
        {
            get
            {
                if (string.IsNullOrWhiteSpace(requiredNamePrefix))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' requires a non-empty texture name prefix.");
                }

                return requiredNamePrefix.Trim();
            }
        }

        /// <summary>
        /// 判断资源是否位于示例规则配置的目录中。
        /// </summary>
        public bool MatchesAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPathPrefix))
            {
                throw new InvalidOperationException(
                    $"Rule settings '{name}' requires a project asset path prefix.");
            }

            var normalizedPrefix = NormalizePath(assetPathPrefix).TrimEnd('/');
            if (!IsProjectAssetPath(normalizedPrefix))
            {
                throw new InvalidOperationException(
                    $"Rule settings '{name}' path prefix must start with 'Assets' or 'Packages'.");
            }

            var normalizedAssetPath = NormalizePath(assetPath);
            return string.Equals(normalizedAssetPath, normalizedPrefix, StringComparison.Ordinal) ||
                   normalizedAssetPath.StartsWith(normalizedPrefix + "/", StringComparison.Ordinal);
        }

        private static bool IsProjectAssetPath(string path)
        {
            return string.Equals(path, "Assets", StringComparison.Ordinal) ||
                   path.StartsWith("Assets/", StringComparison.Ordinal) ||
                   string.Equals(path, "Packages", StringComparison.Ordinal) ||
                   path.StartsWith("Packages/", StringComparison.Ordinal);
        }

        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Trim().Replace('\\', '/');
        }
    }
}
