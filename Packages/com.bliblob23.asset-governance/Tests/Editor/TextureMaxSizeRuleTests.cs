using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class TextureMaxSizeRuleTests
    {
        private const string TestFolder = "Assets/__UnityAssetGovernanceTextureMaxSizeRuleTests";
        private const string NestedFolder = TestFolder + "/Nested";
        private const string OversizedTexturePath = TestFolder + "/Oversized.png";
        private const string CompliantTexturePath = TestFolder + "/Compliant.png";
        private const string NestedTexturePath = NestedFolder + "/Nested.png";
        private const string TextAssetPath = TestFolder + "/NotATexture.txt";
        private const string TestPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAYAAACp8Z5+AAAAD0lEQVR4nGP4jwYYSBcAAHg8P8FY7imoAAAAAElFTkSuQmCC";

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();

            AssetDatabase.CreateFolder("Assets", "__UnityAssetGovernanceTextureMaxSizeRuleTests");
            AssetDatabase.CreateFolder(TestFolder, "Nested");
            WritePngAsset(OversizedTexturePath);
            WritePngAsset(CompliantTexturePath);
            WritePngAsset(NestedTexturePath);
            WriteTextAsset(TextAssetPath, "Not a texture.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureTexture(OversizedTexturePath, 4096);
            ConfigureTexture(CompliantTexturePath, 1024);
            ConfigureTexture(NestedTexturePath, 512);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void CanEvaluate_AcceptsTextureAndRejectsNonTextureAsset()
        {
            var rule = new TextureMaxSizeRule();

            Assert.That(rule.CanEvaluate(ScanSingle(OversizedTexturePath)), Is.True);
            Assert.That(rule.CanEvaluate(ScanSingle(TextAssetPath)), Is.False);
        }

        [Test]
        public void Runner_UsesBuiltInDefaultLimitWhenSettingsAreMissing()
        {
            var result = RuleRunner.Run(AssetScanner.Scan(new[] { OversizedTexturePath }));

            var issue = result.Issues.Single(candidate => candidate.RuleId == "UAG-TEX-003");
            Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Warning));
            Assert.That(issue.CanFix, Is.True);
            Assert.That(issue.Message, Does.Contain("4096"));
            Assert.That(issue.Message, Does.Contain("2048"));
            Assert.That(result.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void Runner_ReturnsNoIssueWhenMaxSizeIsWithinLimit()
        {
            var result = RuleRunner.Run(AssetScanner.Scan(new[] { CompliantTexturePath }));

            Assert.That(
                result.Issues.Any(candidate => candidate.RuleId == "UAG-TEX-003"),
                Is.False);
            Assert.That(result.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void Runner_UsesConfiguredDefaultLimit()
        {
            var settings = CreateSettings(1024);
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { OversizedTexturePath }, profile));

                var issue = result.Issues.Single(candidate => candidate.RuleId == "UAG-TEX-003");
                Assert.That(issue.Message, Does.Contain("1024"));
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_UsesLongestMatchingPathOverride()
        {
            var settings = CreateSettings(
                2048,
                (TestFolder, 1024),
                (NestedFolder, 256));
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { NestedTexturePath }, profile));

                var issue = result.Issues.Single(candidate => candidate.RuleId == "UAG-TEX-003");
                Assert.That(issue.Message, Does.Contain("512"));
                Assert.That(issue.Message, Does.Contain("256"));
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_ReportsInvalidMaximumSizeAsConfigurationExecutionError()
        {
            var settings = CreateSettings(300);
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { OversizedTexturePath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-TEX-003"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Has.Count.EqualTo(1));
                Assert.That(result.ExecutionErrors[0].RuleId, Is.EqualTo("UAG-TEX-003"));
                Assert.That(
                    result.ExecutionErrors[0].Stage,
                    Is.EqualTo(RuleExecutionStage.CanEvaluate));
                Assert.That(result.ExecutionErrors[0].Exception.Message, Does.Contain("power of two"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_SkipsRuleForWhitelistedTexture()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetWhitelistEntries(
                profile,
                (OversizedTexturePath, new[] { "UAG-TEX-003" }));

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { OversizedTexturePath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-TEX-003"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void FixRunner_AppliesConfiguredLimitAndIssueDisappearsAfterRescan()
        {
            var settings = CreateSettings(1024);
            var profile = CreateProfile(settings);

            try
            {
                var context = AssetScanner.Scan(new[] { OversizedTexturePath }, profile).Single();
                var issue = RuleRunner.Run(new[] { context }).Issues
                    .Single(candidate => candidate.RuleId == "UAG-TEX-003");

                var fixResult = FixRunner.Fix(context, issue);
                var importer = (TextureImporter)AssetImporter.GetAtPath(OversizedTexturePath);
                var verificationResult = RuleRunner.Run(
                    AssetScanner.Scan(new[] { OversizedTexturePath }, profile));

                Assert.That(fixResult.Succeeded, Is.True);
                Assert.That(importer.maxTextureSize, Is.EqualTo(1024));
                Assert.That(
                    verificationResult.Issues.Any(candidate => candidate.RuleId == "UAG-TEX-003"),
                    Is.False);
                Assert.That(verificationResult.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void DiscoverRules_FindsMaxSizeRuleWithoutCentralRegistration()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == "UAG-TEX-003"),
                Is.EqualTo(1));
            Assert.That(
                rules.Single(rule => rule.Descriptor.Id == "UAG-TEX-003"),
                Is.TypeOf<TextureMaxSizeRule>());
        }

        private static GovernanceProfile CreateProfile(TextureMaxSizeRuleSettings settings)
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetRuleSettings(profile, settings);
            return profile;
        }

        private static TextureMaxSizeRuleSettings CreateSettings(
            int defaultMaximumSize,
            params (string PathPrefix, int MaximumSize)[] pathOverrides)
        {
            var settings = ScriptableObject.CreateInstance<TextureMaxSizeRuleSettings>();
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("defaultMaximumSize").intValue = defaultMaximumSize;

            var overridesProperty = serializedSettings.FindProperty("pathOverrides");
            overridesProperty.arraySize = pathOverrides.Length;
            for (var index = 0; index < pathOverrides.Length; index++)
            {
                var overrideProperty = overridesProperty.GetArrayElementAtIndex(index);
                overrideProperty.FindPropertyRelative("pathPrefix").stringValue =
                    pathOverrides[index].PathPrefix;
                overrideProperty.FindPropertyRelative("maximumSize").intValue =
                    pathOverrides[index].MaximumSize;
            }

            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static AssetContext ScanSingle(string assetPath)
        {
            return AssetScanner.Scan(new[] { assetPath }).Single();
        }

        private static void ConfigureTexture(string assetPath, int maximumSize)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.maxTextureSize = maximumSize;
            importer.SaveAndReimport();
        }

        private static void WritePngAsset(string assetPath)
        {
            File.WriteAllBytes(
                GetAbsolutePath(assetPath),
                Convert.FromBase64String(TestPngBase64));
        }

        private static void WriteTextAsset(string assetPath, string contents)
        {
            File.WriteAllText(GetAbsolutePath(assetPath), contents);
        }

        private static string GetAbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Unable to determine the Unity project root.");
            }

            return Path.Combine(projectRoot, assetPath);
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
