using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 检查 Texture Importer 的 Max Size 是否超过项目配置的路径阈值。
    /// </summary>
    public sealed class TextureMaxSizeRule : IFixableAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            "UAG-TEX-003",
            "Texture Max Size Must Not Exceed Limit",
            "Texture importer Max Size must not exceed the configured path limit.",
            RuleSeverity.Warning,
            new[] { typeof(Texture) });

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            if (!(context.Importer is TextureImporter))
            {
                return false;
            }

            GetMaximumSize(context);
            return true;
        }

        public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
        {
            var textureImporter = (TextureImporter)context.Importer;
            var maximumSize = GetMaximumSize(context);
            if (textureImporter.maxTextureSize <= maximumSize)
            {
                return Array.Empty<ValidationIssue>();
            }

            return new[]
            {
                new ValidationIssue(
                    DescriptorValue.Id,
                    DescriptorValue.DefaultSeverity,
                    context.AssetPath,
                    $"Texture Max Size is {textureImporter.maxTextureSize}, " +
                    $"but the configured limit is {maximumSize}.")
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
                   textureImporter.maxTextureSize > GetMaximumSize(context);
        }

        public void Fix(AssetContext context, ValidationIssue issue)
        {
            if (!CanFix(context, issue))
            {
                throw new InvalidOperationException(
                    "The texture Max Size issue cannot be fixed in its current state.");
            }

            var textureImporter = (TextureImporter)context.Importer;
            textureImporter.maxTextureSize = GetMaximumSize(context);
            textureImporter.SaveAndReimport();
        }

        private static int GetMaximumSize(AssetContext context)
        {
            if (context.GovernanceProfile == null ||
                !context.GovernanceProfile.TryGetRuleSettings(
                    DescriptorValue.Id,
                    out TextureMaxSizeRuleSettings settings))
            {
                return TextureMaxSizeRuleSettings.DefaultMaximumSize;
            }

            return settings.GetMaximumSize(context.AssetPath);
        }
    }
}
