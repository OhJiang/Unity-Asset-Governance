using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 检查项目配置所识别的 UI 纹理是否关闭了 Mipmap。
    /// </summary>
    public sealed class UiTextureMipmapsDisabledRule : IAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            "UAG-TEX-001",
            "UI Texture Mipmaps Must Be Disabled",
            "UI textures must have mipmaps disabled.",
            RuleSeverity.Error,
            new[] { typeof(Texture2D) });

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            if (!(context.Importer is TextureImporter textureImporter))
            {
                return false;
            }

            var settings = GetSettings(context.GovernanceProfile);
            if (settings == null)
            {
                return textureImporter.textureType == TextureImporterType.Sprite;
            }

            return (settings.IncludeSpriteTextures &&
                    textureImporter.textureType == TextureImporterType.Sprite) ||
                   settings.MatchesUiPath(context.AssetPath);
        }

        public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
        {
            var textureImporter = (TextureImporter)context.Importer;
            if (!textureImporter.mipmapEnabled)
            {
                return Array.Empty<ValidationIssue>();
            }

            return new[]
            {
                new ValidationIssue(
                    DescriptorValue.Id,
                    DescriptorValue.DefaultSeverity,
                    context.AssetPath,
                    "UI texture mipmaps must be disabled.")
            };
        }

        private static UiTextureMipmapsDisabledRuleSettings GetSettings(
            GovernanceProfile governanceProfile)
        {
            if (governanceProfile == null)
            {
                return null;
            }

            governanceProfile.TryGetRuleSettings(
                DescriptorValue.Id,
                out UiTextureMipmapsDisabledRuleSettings settings);
            return settings;
        }
    }
}
