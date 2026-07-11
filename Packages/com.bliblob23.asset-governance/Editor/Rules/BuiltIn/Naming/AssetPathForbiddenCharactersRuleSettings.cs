using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// UAG-NAME-002 的强类型配置，用于补充项目自定义的禁止字符。
    /// 中文字符由规则固定识别，项目专属限制通过此配置扩展。
    /// </summary>
    [CreateAssetMenu(
        fileName = "AssetPathForbiddenCharactersRuleSettings",
        menuName = "Asset Governance/Rule Settings/Asset Path Forbidden Characters Rule")]
    public sealed class AssetPathForbiddenCharactersRuleSettings : AssetRuleSettings
    {
        [SerializeField]
        private string forbiddenCharacters = string.Empty;

        public override string RuleId => "UAG-NAME-002";

        public string ForbiddenCharacters => forbiddenCharacters;

        /// <summary>
        /// 获取经过验证且去重的禁止字符 Unicode 码点。
        /// </summary>
        public IReadOnlyCollection<int> GetForbiddenCodePoints()
        {
            var codePoints = new List<int>();
            var uniqueCodePoints = new HashSet<int>();

            for (var index = 0; index < forbiddenCharacters.Length; index++)
            {
                var codePoint = char.ConvertToUtf32(forbiddenCharacters, index);
                if (char.IsHighSurrogate(forbiddenCharacters[index]))
                {
                    index++;
                }

                if (codePoint == '/' || codePoint == '\\' || codePoint == '.')
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' cannot configure path separators or the " +
                        "file extension separator as forbidden characters.");
                }

                if (!uniqueCodePoints.Add(codePoint))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' contains duplicate forbidden character " +
                        $"'{char.ConvertFromUtf32(codePoint)}'.");
                }

                codePoints.Add(codePoint);
            }

            return new ReadOnlyCollection<int>(codePoints);
        }
    }
}
