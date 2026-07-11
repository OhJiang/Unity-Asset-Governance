using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 检查以 Sprite 类型导入的 UI 纹理是否关闭了 Mipmap。
    /// </summary>
    public sealed class UiTextureMipmapsDisabledRule : IAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            "UAG-TEX-001",
            "UI Texture Mipmaps Must Be Disabled",
            "Textures imported as Sprite must have mipmaps disabled.",
            RuleSeverity.Error,
            new[] { typeof(Texture2D) });

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            return context.Importer is TextureImporter textureImporter &&
                   textureImporter.textureType == TextureImporterType.Sprite;
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
    }
}
