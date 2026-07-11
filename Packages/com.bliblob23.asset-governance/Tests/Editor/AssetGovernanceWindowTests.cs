using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityAssetGovernance.Tests
{
    public sealed class AssetGovernanceWindowTests
    {
        private const string TestFolder = "Assets/__Unity Asset Governance Window Tests";
        private const string FirstAssetPath = TestFolder + "/A.txt";
        private const string SecondAssetPath = TestFolder + "/B.txt";
        private const string ProfilePath = TestFolder + "/GovernanceProfile.asset";

        private Object[] _previousSelection;

        [SetUp]
        public void SetUp()
        {
            _previousSelection = Selection.objects;
            DeleteTestAssets();

            AssetDatabase.CreateFolder("Assets", "__Unity Asset Governance Window Tests");
            WriteTextAsset(FirstAssetPath, "First window test asset.");
            WriteTextAsset(SecondAssetPath, "Second window test asset.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [TearDown]
        public void TearDown()
        {
            Selection.objects = _previousSelection ?? Array.Empty<Object>();
            DeleteTestAssets();
        }

        [Test]
        public void CollectSelectedAssetPaths_IgnoresNonAssetsRemovesDuplicatesAndSorts()
        {
            var firstAsset = AssetDatabase.LoadMainAssetAtPath(FirstAssetPath);
            var secondAsset = AssetDatabase.LoadMainAssetAtPath(SecondAssetPath);
            var sceneObject = new GameObject("Not An Asset");

            try
            {
                Selection.objects = new[] { secondAsset, sceneObject, firstAsset, secondAsset };

                var paths = AssetGovernanceWindow.CollectSelectedAssetPaths();

                Assert.That(paths, Is.EqualTo(new[] { FirstAssetPath, SecondAssetPath }));
            }
            finally
            {
                Object.DestroyImmediate(sceneObject);
            }
        }

        [Test]
        public void ScanSelection_ConnectsSelectionScannerRegistryAndRunner()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FirstAssetPath);
            var window = ScriptableObject.CreateInstance<AssetGovernanceWindow>();

            try
            {
                var succeeded = window.ScanSelection();

                Assert.That(succeeded, Is.True);
                Assert.That(window.LastResult, Is.Not.Null);
                Assert.That(window.LastResult.ExecutionErrors, Is.Empty);
                Assert.That(
                    window.LastResult.Issues.Single(issue => issue.RuleId == "UAG-NAME-001").AssetPath,
                    Is.EqualTo(FirstAssetPath));
                Assert.That(window.StatusMessage, Is.EqualTo("Scanned 1 asset(s)."));
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void ScanSelection_UsesDefaultProfileRuleState()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetRuleStates(profile, ("UAG-NAME-001", false));
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FirstAssetPath);
            var window = ScriptableObject.CreateInstance<AssetGovernanceWindow>();

            try
            {
                var succeeded = window.ScanSelection();

                Assert.That(succeeded, Is.True);
                Assert.That(window.LastResult.ExecutionErrors, Is.Empty);
                Assert.That(
                    window.LastResult.Issues.Any(issue => issue.RuleId == "UAG-NAME-001"),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void ScanSelection_SkipsAssetExcludedByDefaultProfile()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetExcludedPaths(profile, FirstAssetPath);
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FirstAssetPath);
            var window = ScriptableObject.CreateInstance<AssetGovernanceWindow>();

            try
            {
                var succeeded = window.ScanSelection();

                Assert.That(succeeded, Is.True);
                Assert.That(window.LastResult.Issues, Is.Empty);
                Assert.That(window.LastResult.ExecutionErrors, Is.Empty);
                Assert.That(window.StatusMessage, Is.EqualTo("Scanned 0 asset(s)."));
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void ScanSelection_SkipsOnlyWhitelistedRuleFromDefaultProfile()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetWhitelistEntries(
                profile,
                (FirstAssetPath, new[] { "UAG-NAME-001" }));
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FirstAssetPath);
            var window = ScriptableObject.CreateInstance<AssetGovernanceWindow>();

            try
            {
                var succeeded = window.ScanSelection();

                Assert.That(succeeded, Is.True);
                Assert.That(window.LastResult.Issues, Is.Empty);
                Assert.That(window.LastResult.ExecutionErrors, Is.Empty);
                Assert.That(window.StatusMessage, Is.EqualTo("Scanned 1 asset(s)."));
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void ScanSelection_DisplaysProfileSeverityOverride()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetRuleStatesWithSeverity(
                profile,
                ("UAG-NAME-001", true, true, RuleSeverity.Error));
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FirstAssetPath);
            var window = ScriptableObject.CreateInstance<AssetGovernanceWindow>();

            try
            {
                var succeeded = window.ScanSelection();

                Assert.That(succeeded, Is.True);
                Assert.That(window.LastResult.ExecutionErrors, Is.Empty);
                Assert.That(
                    window.LastResult.Issues.Single(issue => issue.RuleId == "UAG-NAME-001").Severity,
                    Is.EqualTo(RuleSeverity.Error));
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void LocateAsset_SelectsTheIssueAsset()
        {
            var expectedAsset = AssetDatabase.LoadMainAssetAtPath(SecondAssetPath);
            Selection.objects = Array.Empty<Object>();

            AssetGovernanceWindow.LocateAsset(SecondAssetPath);

            Assert.That(Selection.activeObject, Is.SameAs(expectedAsset));
        }

        [Test]
        public void ScanSelection_ReportsWhenNoAssetIsSelected()
        {
            Selection.objects = Array.Empty<Object>();
            var window = ScriptableObject.CreateInstance<AssetGovernanceWindow>();

            try
            {
                var succeeded = window.ScanSelection();

                Assert.That(succeeded, Is.False);
                Assert.That(window.LastResult, Is.Null);
                Assert.That(window.StatusMessage, Is.EqualTo("No assets or folders are selected."));
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
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
