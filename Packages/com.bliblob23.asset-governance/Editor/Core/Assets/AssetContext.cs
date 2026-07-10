using System;
using UnityEditor;

namespace UnityAssetGovernance
{
    /// <summary>
    /// Read-only information supplied to a rule for one asset evaluation.
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
        }

        public string AssetGuid { get; }

        public string AssetPath { get; }

        public Type AssetType { get; }

        /// <summary>
        /// Gets the loaded asset when the scanner has one available; otherwise null.
        /// </summary>
        public UnityEngine.Object Asset { get; }

        /// <summary>
        /// Gets the asset importer when one exists for the asset; otherwise null.
        /// </summary>
        public AssetImporter Importer { get; }

        public BuildTarget BuildTarget { get; }
    }
}
