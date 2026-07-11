using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// UAG-TEX-001 的强类型配置，用于定义项目如何识别 UI 纹理。
    /// </summary>
    [CreateAssetMenu(
        fileName = "UiTextureMipmapRuleSettings",
        menuName = "Asset Governance/Rule Settings/UI Texture Mipmap Rule")]
    public sealed class UiTextureMipmapsDisabledRuleSettings : AssetRuleSettings
    {
        [SerializeField]
        private bool includeSpriteTextures = true;

        [SerializeField]
        private List<string> uiPathPrefixes = new List<string>();

        public override string RuleId => "UAG-TEX-001";

        /// <summary>
        /// 获取是否继续把 Sprite 导入类型视为 UI 纹理。
        /// </summary>
        public bool IncludeSpriteTextures => includeSpriteTextures;

        /// <summary>
        /// 获取项目配置的 UI 资源目录前缀只读视图。
        /// </summary>
        public IReadOnlyList<string> UiPathPrefixes =>
            new ReadOnlyCollection<string>(uiPathPrefixes);

        /// <summary>
        /// 判断资源路径是否位于任一已配置的 UI 目录中。
        /// </summary>
        public bool MatchesUiPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("An asset path is required.", nameof(assetPath));
            }

            var normalizedAssetPath = NormalizePath(assetPath);
            foreach (var pathPrefix in uiPathPrefixes)
            {
                if (string.IsNullOrWhiteSpace(pathPrefix))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' contains an empty UI path prefix.");
                }

                var normalizedPrefix = NormalizePath(pathPrefix).TrimEnd('/');
                if (string.Equals(normalizedAssetPath, normalizedPrefix, StringComparison.Ordinal) ||
                    normalizedAssetPath.StartsWith(normalizedPrefix + "/", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizePath(string path)
        {
            return path.Trim().Replace('\\', '/');
        }
    }
}
