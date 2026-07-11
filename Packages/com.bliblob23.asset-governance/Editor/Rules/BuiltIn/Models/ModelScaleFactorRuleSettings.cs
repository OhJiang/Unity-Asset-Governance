using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 为一个模型资源或文件夹路径定义 Scale Factor 覆盖值。
    /// </summary>
    [Serializable]
    public sealed class ModelScaleFactorPathOverride
    {
        [SerializeField]
        private string pathPrefix = "Assets";

        [SerializeField]
        private float expectedScaleFactor = ModelScaleFactorRuleSettings.DefaultExpectedScaleFactor;

        public string PathPrefix => pathPrefix;

        public float ExpectedScaleFactor => expectedScaleFactor;
    }

    /// <summary>
    /// UAG-MODEL-001 的强类型配置，支持默认值和按路径细化的 Scale Factor。
    /// </summary>
    [CreateAssetMenu(
        fileName = "ModelScaleFactorRuleSettings",
        menuName = "Asset Governance/Rule Settings/Model Scale Factor Rule")]
    public sealed class ModelScaleFactorRuleSettings : AssetRuleSettings
    {
        public const float DefaultExpectedScaleFactor = 1f;

        [SerializeField]
        private float defaultExpectedScaleFactor = DefaultExpectedScaleFactor;

        [SerializeField]
        private List<ModelScaleFactorPathOverride> pathOverrides =
            new List<ModelScaleFactorPathOverride>();

        public override string RuleId => "UAG-MODEL-001";

        public float DefaultScaleFactor => defaultExpectedScaleFactor;

        public IReadOnlyList<ModelScaleFactorPathOverride> PathOverrides =>
            new ReadOnlyCollection<ModelScaleFactorPathOverride>(pathOverrides);

        /// <summary>
        /// 获取指定模型路径应使用的 Scale Factor。多个路径同时匹配时，最长路径优先。
        /// </summary>
        public float GetExpectedScaleFactor(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("An asset path is required.", nameof(assetPath));
            }

            ValidateScaleFactor(defaultExpectedScaleFactor, "default Scale Factor");

            var normalizedAssetPath = NormalizePath(assetPath);
            var configuredPaths = new HashSet<string>(StringComparer.Ordinal);
            var matchedPathLength = -1;
            var expectedScaleFactor = defaultExpectedScaleFactor;

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

                ValidateScaleFactor(
                    pathOverride.ExpectedScaleFactor,
                    $"Scale Factor for path '{normalizedPrefix}'");

                if (normalizedPrefix.Length > matchedPathLength &&
                    PathMatches(normalizedAssetPath, normalizedPrefix))
                {
                    matchedPathLength = normalizedPrefix.Length;
                    expectedScaleFactor = pathOverride.ExpectedScaleFactor;
                }
            }

            return expectedScaleFactor;
        }

        private static void ValidateScaleFactor(float scaleFactor, string fieldName)
        {
            if (float.IsNaN(scaleFactor) || float.IsInfinity(scaleFactor) || scaleFactor <= 0f)
            {
                throw new InvalidOperationException(
                    $"Model {fieldName} must be a finite value greater than zero.");
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
