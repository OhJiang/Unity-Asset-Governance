using System;
using UnityEditor;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 规则检查单个资源时使用的只读上下文信息。
    /// </summary>
    public sealed class AssetContext
    {
        public AssetContext(
            string assetGuid,
            string assetPath,
            Type assetType,
            UnityEngine.Object asset,
            AssetImporter importer,
            BuildTarget buildTarget)
            : this(
                assetGuid,
                assetPath,
                assetType,
                asset,
                importer,
                buildTarget,
                null)
        {
        }

        public AssetContext(
            string assetGuid,
            string assetPath,
            Type assetType,
            UnityEngine.Object asset,
            AssetImporter importer,
            BuildTarget buildTarget,
            GovernanceProfile governanceProfile)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("An asset path is required.", nameof(assetPath));
            }

            AssetGuid = assetGuid ?? string.Empty;
            AssetPath = assetPath;
            AssetType = assetType ?? throw new ArgumentNullException(nameof(assetType));
            Asset = asset;
            Importer = importer;
            BuildTarget = buildTarget;
            GovernanceProfile = governanceProfile;
        }

        public string AssetGuid { get; }

        public string AssetPath { get; }

        public Type AssetType { get; }

        /// <summary>
        /// 获取扫描器已经加载的资源对象；未加载时为空引用。
        /// </summary>
        public UnityEngine.Object Asset { get; }

        /// <summary>
        /// 获取资源对应的导入器；资源不存在导入器时为空引用。
        /// </summary>
        public AssetImporter Importer { get; }

        public BuildTarget BuildTarget { get; }

        /// <summary>
        /// 获取本次扫描使用的项目治理配置；项目未创建配置时为空引用。
        /// </summary>
        public GovernanceProfile GovernanceProfile { get; }
    }
}
