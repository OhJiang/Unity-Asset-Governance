using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// UAG-AUDIO-001 的强类型配置，用于识别短音效并指定自动修复使用的非 Streaming 类型。
    /// </summary>
    [CreateAssetMenu(
        fileName = "ShortAudioStreamingRuleSettings",
        menuName = "Asset Governance/Rule Settings/Short Audio Streaming Rule")]
    public sealed class ShortAudioStreamingRuleSettings : AssetRuleSettings
    {
        [SerializeField]
        private List<string> shortAudioPathPrefixes = new List<string>();

        [SerializeField]
        private AudioClipLoadType replacementLoadType = AudioClipLoadType.DecompressOnLoad;

        public override string RuleId => "UAG-AUDIO-001";

        public IReadOnlyList<string> ShortAudioPathPrefixes =>
            new ReadOnlyCollection<string>(shortAudioPathPrefixes);

        public AudioClipLoadType ReplacementLoadType => replacementLoadType;

        /// <summary>
        /// 判断资源路径是否位于任一已配置的短音效目录中。
        /// </summary>
        public bool MatchesShortAudioPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("An asset path is required.", nameof(assetPath));
            }

            if (replacementLoadType == AudioClipLoadType.Streaming)
            {
                throw new InvalidOperationException(
                    $"Rule settings '{name}' replacement Load Type cannot be Streaming.");
            }

            var normalizedAssetPath = NormalizePath(assetPath);
            var configuredPaths = new HashSet<string>(StringComparer.Ordinal);
            var matches = false;

            foreach (var pathPrefix in shortAudioPathPrefixes)
            {
                if (string.IsNullOrWhiteSpace(pathPrefix))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' contains an empty short audio path prefix.");
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
