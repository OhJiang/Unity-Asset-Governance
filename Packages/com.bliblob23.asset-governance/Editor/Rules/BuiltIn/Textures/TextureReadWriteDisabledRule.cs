using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 检查纹理是否关闭了 Read/Write，例外资源由项目级按规则白名单管理。
    /// </summary>
    public sealed class TextureReadWriteDisabledRule : IFixableAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            "UAG-TEX-002",
            "Texture Read/Write Must Be Disabled",
            "Textures must have Read/Write disabled unless explicitly whitelisted.",
            RuleSeverity.Warning,
            new[] { typeof(Texture) });

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            return context.Importer is TextureImporter;
        }

        public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
        {
            var textureImporter = (TextureImporter)context.Importer;
            if (!textureImporter.isReadable)
            {
                return Array.Empty<ValidationIssue>();
            }

            return new[]
            {
                new ValidationIssue(
                    DescriptorValue.Id,
                    DescriptorValue.DefaultSeverity,
                    context.AssetPath,
                    "Texture Read/Write must be disabled unless the asset is explicitly whitelisted.")
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
                   textureImporter.isReadable;
        }

        public void Fix(AssetContext context, ValidationIssue issue)
        {
            if (!CanFix(context, issue))
            {
                throw new InvalidOperationException(
                    "The texture Read/Write issue cannot be fixed in its current state.");
            }

            var textureImporter = (TextureImporter)context.Importer;
            textureImporter.isReadable = false;
            textureImporter.SaveAndReimport();
        }
    }
}
