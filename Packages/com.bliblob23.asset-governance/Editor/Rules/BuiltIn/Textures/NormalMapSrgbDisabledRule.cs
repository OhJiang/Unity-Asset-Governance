using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 检查法线贴图是否关闭了 sRGB 采样。
    /// </summary>
    public sealed class NormalMapSrgbDisabledRule : IFixableAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            "UAG-TEX-004",
            "Normal Map sRGB Must Be Disabled",
            "Normal maps must have sRGB texture sampling disabled.",
            RuleSeverity.Error,
            new[] { typeof(Texture) });

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            return context.Importer is TextureImporter textureImporter &&
                   textureImporter.textureType == TextureImporterType.NormalMap;
        }

        public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
        {
            var textureImporter = (TextureImporter)context.Importer;
            if (!textureImporter.sRGBTexture)
            {
                return Array.Empty<ValidationIssue>();
            }

            return new[]
            {
                new ValidationIssue(
                    DescriptorValue.Id,
                    DescriptorValue.DefaultSeverity,
                    context.AssetPath,
                    "Normal map sRGB texture sampling must be disabled.")
            };
        }

        public bool CanFix(AssetContext context, ValidationIssue issue)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            return string.Equals(issue.RuleId, DescriptorValue.Id, StringComparison.Ordinal) &&
                   string.Equals(issue.AssetPath, context.AssetPath, StringComparison.Ordinal) &&
                   context.Importer is TextureImporter textureImporter &&
                   textureImporter.textureType == TextureImporterType.NormalMap &&
                   textureImporter.sRGBTexture;
        }

        public void Fix(AssetContext context, ValidationIssue issue)
        {
            if (!CanFix(context, issue))
            {
                throw new InvalidOperationException(
                    "The normal map sRGB issue cannot be fixed in its current state.");
            }

            var textureImporter = (TextureImporter)context.Importer;
            textureImporter.sRGBTexture = false;
            textureImporter.SaveAndReimport();
        }
    }
}
