using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 为一个资源或文件夹路径定义 Texture Max Size 覆盖值。
    /// </summary>
    [Serializable]
    public sealed class TextureMaxSizePathOverride
    {
        [SerializeField]
        private string pathPrefix = "Assets";

        [SerializeField]
        private int maximumSize = TextureMaxSizeRuleSettings.DefaultMaximumSize;

        public string PathPrefix => pathPrefix;

        public int MaximumSize => maximumSize;
    }

    /// <summary>
    /// UAG-TEX-003 的强类型配置，支持默认阈值和按路径细化的阈值。
    /// </summary>
    [CreateAssetMenu(
        fileName = "TextureMaxSizeRuleSettings",
        menuName = "Asset Governance/Rule Settings/Texture Max Size Rule")]
    public sealed class TextureMaxSizeRuleSettings : AssetRuleSettings
    {
        public const int DefaultMaximumSize = 2048;

        [SerializeField]
        private int defaultMaximumSize = DefaultMaximumSize;

        [SerializeField]
        private List<TextureMaxSizePathOverride> pathOverrides =
            new List<TextureMaxSizePathOverride>();

        public override string RuleId => "UAG-TEX-003";

        public int DefaultMaxSize => defaultMaximumSize;

        public IReadOnlyList<TextureMaxSizePathOverride> PathOverrides =>
            new ReadOnlyCollection<TextureMaxSizePathOverride>(pathOverrides);

        /// <summary>
        /// 获取指定资源路径应使用的 Max Size。多个路径同时匹配时，最长路径优先。
        /// </summary>
        public int GetMaximumSize(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("An asset path is required.", nameof(assetPath));
            }

            ValidateMaximumSize(defaultMaximumSize, "default Max Size");

            var normalizedAssetPath = NormalizePath(assetPath);
            var configuredPaths = new HashSet<string>(StringComparer.Ordinal);
            var matchedPathLength = -1;
            var maximumSize = defaultMaximumSize;

            foreach (var pathOverride in pathOverrides)
            {
                if (pathOverride == null)
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' contains a missing path override.");
                }

                if (string.IsNullOrWhiteSpace(pathOverride.PathPrefix))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' contains an empty path prefix.");
                }

                var normalizedPrefix = NormalizePath(pathOverride.PathPrefix).TrimEnd('/');
                if (!IsProjectAssetPath(normalizedPrefix))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' contains invalid path prefix " +
                        $"'{pathOverride.PathPrefix}'. Paths must start with 'Assets' or 'Packages'.");
                }

                if (!configuredPaths.Add(normalizedPrefix))
                {
                    throw new InvalidOperationException(
                        $"Rule settings '{name}' contains duplicate path prefix " +
                        $"'{normalizedPrefix}'.");
                }

                ValidateMaximumSize(
                    pathOverride.MaximumSize,
                    $"Max Size for path '{normalizedPrefix}'");

                if (PathMatches(normalizedAssetPath, normalizedPrefix) &&
                    normalizedPrefix.Length > matchedPathLength)
                {
                    matchedPathLength = normalizedPrefix.Length;
                    maximumSize = pathOverride.MaximumSize;
                }
            }

            return maximumSize;
        }

        private static void ValidateMaximumSize(int maximumSize, string fieldName)
        {
            if (maximumSize < 32 || maximumSize > 16384 ||
                (maximumSize & (maximumSize - 1)) != 0)
            {
                throw new InvalidOperationException(
                    $"Texture {fieldName} must be a power of two between 32 and 16384.");
            }
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
