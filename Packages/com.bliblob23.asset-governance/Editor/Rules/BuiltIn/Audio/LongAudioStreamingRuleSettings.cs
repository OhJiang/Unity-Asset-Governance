using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// UAG-AUDIO-002 的强类型配置，用于通过项目路径识别长音频和 BGM。
    /// </summary>
    [CreateAssetMenu(
        fileName = "LongAudioStreamingRuleSettings",
        menuName = "Asset Governance/Rule Settings/Long Audio Streaming Rule")]
    public sealed class LongAudioStreamingRuleSettings : AssetRuleSettings
    {
        [SerializeField]
        private List<string> longAudioPathPrefixes = new List<string>();

        public override string RuleId => "UAG-AUDIO-002";

        public IReadOnlyList<string> LongAudioPathPrefixes =>
            new ReadOnlyCollection<string>(longAudioPathPrefixes);

        /// <summary>
        /// 判断资源路径是否位于任一已配置的长音频或 BGM 目录中。
        /// </summary>
        public bool MatchesLongAudioPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("An asset path is required.", nameof(assetPath));
            }

            var normalizedAssetPath = NormalizePath(assetPath);
            var configuredPaths = new HashSet<string>(StringComparer.Ordinal);
            var matches = false;

            foreach (var pathPrefix in longAudioPathPrefixes)
            {
                if (string.IsNullOrWhiteSpace(pathPrefix))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' contains an empty long audio path prefix.");
                }

                var normalizedPrefix = NormalizePath(pathPrefix).TrimEnd('/');
                if (!IsProjectAssetPath(normalizedPrefix))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' contains invalid path prefix '{pathPrefix}'. " +
                        "Paths must start with 'Assets' or 'Packages'.");
                }

                if (!configuredPaths.Add(normalizedPrefix))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' contains duplicate path prefix " +
                        $"'{normalizedPrefix}'.");
                }

                if (PathMatches(normalizedAssetPath, normalizedPrefix))
                {
                    matches = true;
                }
            }

            return matches;
        }

        private static bool PathMatches(string assetPath, string configuredPath)
        {
            return string.Equals(assetPath, configuredPath, StringComparison.Ordinal) ||
                   assetPath.StartsWith(configuredPath + "/", StringComparison.Ordinal);
        }

        private static bool IsProjectAssetPath(string path)
        {
            return string.Equals(path, "Assets", StringComparison.Ordinal) ||
                   path.StartsWith("Assets/", StringComparison.Ordinal) ||
                   string.Equals(path, "Packages", StringComparison.Ordinal) ||
                   path.StartsWith("Packages/", StringComparison.Ordinal);
        }

        private static string NormalizePath(string path)
        {
            return path.Trim().Replace('\\', '/');
        }
    }
}
