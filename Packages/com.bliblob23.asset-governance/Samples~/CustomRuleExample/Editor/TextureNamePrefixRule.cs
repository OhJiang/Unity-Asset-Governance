using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityAssetGovernance.CustomRuleExample
{
    /// <summary>
    /// 演示第三方程序集如何通过公共接口注册规则，并读取自己的强类型配置。
    /// </summary>
    public sealed class TextureNamePrefixRule : IAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            TextureNamePrefixRuleSettings.StableRuleId,
            "Sample Textures Must Use The Configured Name Prefix",
            "Textures under the configured project path must use the configured file name prefix.",
            RuleSeverity.Warning,
            new[] { typeof(Texture2D) });

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            if (!typeof(Texture2D).IsAssignableFrom(context.AssetType))
            {
                return false;
            }

            var settings = GetSettings(context);
            if (settings == null || !settings.MatchesAssetPath(context.AssetPath))
            {
                return false;
            }

            _ = settings.RequiredNamePrefix;
            return true;
        }

        public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
        {
            var settings = GetSettings(context);
            if (settings == null ||
                !typeof(Texture2D).IsAssignableFrom(context.AssetType) ||
                !settings.MatchesAssetPath(context.AssetPath))
            {
                return Array.Empty<ValidationIssue>();
            }

            var requiredNamePrefix = settings.RequiredNamePrefix;
            var assetName = Path.GetFileNameWithoutExtension(context.AssetPath);
            if (assetName.StartsWith(requiredNamePrefix, StringComparison.Ordinal))
            {
                return Array.Empty<ValidationIssue>();
            }

            return new[]
            {
                new ValidationIssue(
                    DescriptorValue.Id,
                    DescriptorValue.DefaultSeverity,
                    context.AssetPath,
                    $"Texture name must start with '{requiredNamePrefix}'.")
            };
        }

        private static TextureNamePrefixRuleSettings GetSettings(AssetContext context)
        {
            if (context.GovernanceProfile == null ||
                !context.GovernanceProfile.TryGetRuleSettings(
                    DescriptorValue.Id,
                    out TextureNamePrefixRuleSettings settings))
            {
                return null;
            }

            return settings;
        }
    }
}
