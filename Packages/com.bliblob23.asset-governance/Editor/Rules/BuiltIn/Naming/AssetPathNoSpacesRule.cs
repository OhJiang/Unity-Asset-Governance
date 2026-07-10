using System;
using System.Collections.Generic;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 检查资源路径及文件名中是否包含普通空格。
    /// </summary>
    public sealed class AssetPathNoSpacesRule : IAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            "UAG-NAME-001",
            "Asset Paths Must Not Contain Spaces",
            "Asset paths and file names must not contain spaces.",
            RuleSeverity.Warning);

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            return true;
        }

        public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
        {
            if (!context.AssetPath.Contains(" "))
            {
                return Array.Empty<ValidationIssue>();
            }

            return new[]
            {
                new ValidationIssue(
                    DescriptorValue.Id,
                    DescriptorValue.DefaultSeverity,
                    context.AssetPath,
                    "Asset path must not contain spaces.")
            };
        }
    }
}
