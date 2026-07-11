using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 将资源路径或文件夹转换为规则可以检查的资源上下文。
    /// </summary>
    public static class AssetScanner
    {
        /// <summary>
        /// 扫描指定资源和文件夹，并按资源路径返回确定性排序的只读结果。
        /// 文件夹会递归展开，重复资源只返回一次。
        /// </summary>
        public static IReadOnlyList<AssetContext> Scan(IEnumerable<string> assetPaths)
        {
            if (assetPaths == null)
            {
                throw new ArgumentNullException(nameof(assetPaths));
            }

            return Scan(assetPaths, GovernanceProfileLocator.LoadDefault());
        }

        /// <summary>
        /// 使用显式指定的 Profile 扫描资源，便于工具入口、CI 和测试控制配置来源。
        /// </summary>
        public static IReadOnlyList<AssetContext> Scan(
            IEnumerable<string> assetPaths,
            GovernanceProfile governanceProfile)
        {
            if (assetPaths == null)
            {
                throw new ArgumentNullException(nameof(assetPaths));
            }

            var uniqueAssetPaths = new HashSet<string>(StringComparer.Ordinal);

            foreach (var assetPath in assetPaths)
            {
                var normalizedPath = NormalizeAndValidatePath(assetPath);

                if (AssetDatabase.IsValidFolder(normalizedPath))
                {
                    AddFolderAssets(normalizedPath, uniqueAssetPaths);
                    continue;
                }

                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(normalizedPath)))
                {
                    throw new ArgumentException(
                        $"Asset path '{normalizedPath}' does not exist.",
                        nameof(assetPaths));
                }

                uniqueAssetPaths.Add(normalizedPath);
            }

            var sortedAssetPaths = new List<string>(uniqueAssetPaths);
            sortedAssetPaths.Sort(StringComparer.Ordinal);

            var contexts = new List<AssetContext>(sortedAssetPaths.Count);
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;

            foreach (var assetPath in sortedAssetPaths)
            {
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (assetType == null)
                {
                    throw new InvalidOperationException(
                        $"Unable to determine the main asset type for '{assetPath}'.");
                }

                contexts.Add(new AssetContext(
                    AssetDatabase.AssetPathToGUID(assetPath),
                    assetPath,
                    assetType,
                    null,
                    AssetImporter.GetAtPath(assetPath),
                    buildTarget,
                    governanceProfile));
            }

            return new ReadOnlyCollection<AssetContext>(contexts);
        }

        private static string NormalizeAndValidatePath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException(
                    "Asset paths cannot contain null or empty values.",
                    nameof(assetPath));
            }

            var normalizedPath = assetPath.Trim().Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(normalizedPath))
            {
                throw new ArgumentException("An asset path is required.", nameof(assetPath));
            }

            return normalizedPath;
        }

        private static void AddFolderAssets(
            string folderPath,
            ISet<string> assetPaths)
        {
            foreach (var assetGuid in AssetDatabase.FindAssets(string.Empty, new[] { folderPath }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                assetPaths.Add(assetPath);
            }
        }
    }
}
