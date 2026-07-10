using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class AssetScannerTests
    {
        private const string TestFolder = "Assets/__UnityAssetGovernanceAssetScannerTests";
        private const string NestedFolder = TestFolder + "/Nested";
        private const string FirstAssetPath = TestFolder + "/A.txt";
        private const string SecondAssetPath = NestedFolder + "/B.txt";

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();

            AssetDatabase.CreateFolder("Assets", "__UnityAssetGovernanceAssetScannerTests");
            AssetDatabase.CreateFolder(TestFolder, "Nested");
            WriteTextAsset(FirstAssetPath, "First test asset.");
            WriteTextAsset(SecondAssetPath, "Second test asset.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void Scan_CreatesContextForDirectAssetWithoutLoadingObject()
        {
            var contexts = AssetScanner.Scan(new[] { FirstAssetPath });

            Assert.That(contexts, Has.Count.EqualTo(1));

            var context = contexts[0];
            Assert.That(context.AssetGuid, Is.EqualTo(AssetDatabase.AssetPathToGUID(FirstAssetPath)));
            Assert.That(context.AssetPath, Is.EqualTo(FirstAssetPath));
            Assert.That(context.AssetType, Is.EqualTo(typeof(TextAsset)));
            Assert.That(context.Asset, Is.Null);
            Assert.That(context.Importer, Is.Not.Null);
            Assert.That(context.BuildTarget, Is.EqualTo(EditorUserBuildSettings.activeBuildTarget));
        }

        [Test]
        public void Scan_ExpandsFoldersRemovesDuplicatesAndSortsByPath()
        {
            var contexts = AssetScanner.Scan(new[]
            {
                NestedFolder + "\\B.txt",
                TestFolder,
                FirstAssetPath
            });

            Assert.That(
                contexts.Select(context => context.AssetPath),
                Is.EqualTo(new[] { FirstAssetPath, SecondAssetPath }));
        }

        [Test]
        public void Scan_ReturnsReadOnlyCollection()
        {
            var contexts = AssetScanner.Scan(new[] { FirstAssetPath });
            var mutableView = (IList<AssetContext>)contexts;

            Assert.That(
                () => mutableView.Add(contexts[0]),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void Scan_RejectsInvalidAssetPath()
        {
            Assert.That(
                () => AssetScanner.Scan(new[] { TestFolder + "/Missing.txt" }),
                Throws.ArgumentException.With.Message.Contains("does not exist"));
        }

        private static void WriteTextAsset(string assetPath, string contents)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Unable to determine the Unity project root.");
            }

            File.WriteAllText(Path.Combine(projectRoot, assetPath), contents);
        }

        private static void DeleteTestAssets()
        {
            if (AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.DeleteAsset(TestFolder);
            }
        }
    }
}
