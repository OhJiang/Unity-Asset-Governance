using System;
using System.Collections.Generic;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 检查资源路径及文件名中是否包含中文或项目配置的禁止字符。
    /// </summary>
    public sealed class AssetPathForbiddenCharactersRule : IAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            "UAG-NAME-002",
            "Asset Paths Must Not Contain Chinese Or Forbidden Characters",
            "Asset paths and file names must not contain Chinese or configured forbidden characters.",
            RuleSeverity.Warning);

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            GetConfiguredForbiddenCodePoints(context);
            return true;
        }

        public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
        {
            var configuredCodePoints = GetConfiguredForbiddenCodePoints(context);
            var foundCodePoints = new HashSet<int>();
            var displayCharacters = new List<string>();

            for (var index = 0; index < context.AssetPath.Length; index++)
            {
                var codePoint = char.ConvertToUtf32(context.AssetPath, index);
                if (char.IsHighSurrogate(context.AssetPath[index]))
                {
                    index++;
                }

                if ((IsChineseCodePoint(codePoint) || configuredCodePoints.Contains(codePoint)) &&
                    foundCodePoints.Add(codePoint))
                {
                    displayCharacters.Add($"'{char.ConvertFromUtf32(codePoint)}'");
                }
            }

            if (displayCharacters.Count == 0)
            {
                return Array.Empty<ValidationIssue>();
            }

            return new[]
            {
                new ValidationIssue(
                    DescriptorValue.Id,
                    DescriptorValue.DefaultSeverity,
                    context.AssetPath,
                    "Asset path contains Chinese or configured forbidden characters: " +
                    $"{string.Join(", ", displayCharacters)}.")
            };
        }

        private static HashSet<int> GetConfiguredForbiddenCodePoints(AssetContext context)
        {
            if (context.GovernanceProfile == null ||
                !context.GovernanceProfile.TryGetRuleSettings(
                    DescriptorValue.Id,
                    out AssetPathForbiddenCharactersRuleSettings settings))
            {
                return new HashSet<int>();
            }

            return new HashSet<int>(settings.GetForbiddenCodePoints());
        }

        private static bool IsChineseCodePoint(int codePoint)
        {
            return codePoint >= 0x3400 && codePoint <= 0x4DBF ||
                   codePoint >= 0x4E00 && codePoint <= 0x9FFF ||
                   codePoint >= 0xF900 && codePoint <= 0xFAFF ||
                   codePoint >= 0x20000 && codePoint <= 0x2FA1F;
        }
    }
}
