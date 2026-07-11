using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class UiTextureMipmapsDisabledRuleTests
    {
        private const string TestFolder = "Assets/__UnityAssetGovernanceUiTextureRuleTests";
        private const string MipmapsEnabledPath = TestFolder + "/MipmapsEnabled.png";
        private const string MipmapsDisabledPath = TestFolder + "/MipmapsDisabled.png";
        private const string DefaultTexturePath = TestFolder + "/DefaultTexture.png";
        private const string TestPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAYAAACp8Z5+AAAAD0lEQVR4nGP4jwYYSBcAAHg8P8FY7imoAAAAAElFTkSuQmCC";

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();

            AssetDatabase.CreateFolder("Assets", "__UnityAssetGovernanceUiTextureRuleTests");
            WritePngAsset(MipmapsEnabledPath);
            WritePngAsset(MipmapsDisabledPath);
            WritePngAsset(DefaultTexturePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureTexture(MipmapsEnabledPath, TextureImporterType.Sprite, true);
            ConfigureTexture(MipmapsDisabledPath, TextureImporterType.Sprite, false);
            ConfigureTexture(DefaultTexturePath, TextureImporterType.Default, true);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void CanEvaluate_AcceptsSpriteTexturesAndRejectsDefaultTextures()
        {
            var rule = new UiTextureMipmapsDisabledRule();
            var spriteContext = ScanSingle(MipmapsEnabledPath);
            var defaultContext = ScanSingle(DefaultTexturePath);

            Assert.That(rule.CanEvaluate(spriteContext), Is.True);
            Assert.That(rule.CanEvaluate(defaultContext), Is.False);
        }


        [Test]
        public void CanEvaluate_AcceptsDefaultTextureInsideConfiguredUiPath()
        {
            var rule = new UiTextureMipmapsDisabledRule();
            var settings = CreateSettings(false, TestFolder);
            var profile = CreateProfile(settings);

            try
            {
                var context = AssetScanner.Scan(new[] { DefaultTexturePath }, profile).Single();

                Assert.That(rule.CanEvaluate(context), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void CanEvaluate_CanDisableSpriteClassificationThroughSettings()
        {
            var rule = new UiTextureMipmapsDisabledRule();
            var settings = CreateSettings(false);
            var profile = CreateProfile(settings);

            try
            {
                var context = AssetScanner.Scan(new[] { MipmapsEnabledPath }, profile).Single();

                Assert.That(rule.CanEvaluate(context), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void ScannerAndRunner_UseStronglyTypedUiPathSettings()
        {
            var settings = CreateSettings(false, TestFolder);
            var profile = CreateProfile(settings);

            try
            {
                var contexts = AssetScanner.Scan(new[] { DefaultTexturePath }, profile);
                var result = RuleRunner.Run(contexts);

                Assert.That(result.ExecutionErrors, Is.Empty);
                Assert.That(
                    result.Issues.Single(issue => issue.RuleId == "UAG-TEX-001").AssetPath,
                    Is.EqualTo(DefaultTexturePath));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_ReportsInvalidUiPathConfigurationAsExecutionError()
        {
            var settings = CreateSettings(false, string.Empty);
            var profile = CreateProfile(settings);

            try
            {
                var contexts = AssetScanner.Scan(new[] { DefaultTexturePath }, profile);
                var result = RuleRunner.Run(contexts);

                Assert.That(result.Issues, Is.Empty);
                Assert.That(result.ExecutionErrors, Has.Count.EqualTo(1));
                Assert.That(result.ExecutionErrors[0].RuleId, Is.EqualTo("UAG-TEX-001"));
                Assert.That(result.ExecutionErrors[0].Stage, Is.EqualTo(RuleExecutionStage.CanEvaluate));
                Assert.That(result.ExecutionErrors[0].Exception.Message, Does.Contain("empty UI path prefix"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Evaluate_ReturnsNoIssueWhenMipmapsAreDisabled()
        {
            var rule = new UiTextureMipmapsDisabledRule();
            var context = ScanSingle(MipmapsDisabledPath);

            var issues = rule.Evaluate(context);

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void Evaluate_ReturnsErrorWhenMipmapsAreEnabled()
        {
            var rule = new UiTextureMipmapsDisabledRule();
            var context = ScanSingle(MipmapsEnabledPath);

            var issue = rule.Evaluate(context).Single();

            Assert.That(issue.RuleId, Is.EqualTo("UAG-TEX-001"));
            Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Error));
            Assert.That(issue.AssetPath, Is.EqualTo(MipmapsEnabledPath));
            Assert.That(issue.Message, Is.EqualTo("UI texture mipmaps must be disabled."));
        }

        [Test]
        public void DiscoverRules_FindsTextureRuleWithoutCentralRegistration()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == "UAG-TEX-001"),
                Is.EqualTo(1));
            Assert.That(
                rules.Single(rule => rule.Descriptor.Id == "UAG-TEX-001"),
                Is.TypeOf<UiTextureMipmapsDisabledRule>());
        }

        [Test]
        public void ScannerRegistryAndRunner_ProduceMipmapIssue()
        {
            var contexts = AssetScanner.Scan(new[] { MipmapsEnabledPath });

            var result = RuleRunner.Run(contexts);

            Assert.That(result.ExecutionErrors, Is.Empty);
            Assert.That(
                result.Issues.Single(issue => issue.RuleId == "UAG-TEX-001").AssetPath,
                Is.EqualTo(MipmapsEnabledPath));
        }


        private static GovernanceProfile CreateProfile(
            UiTextureMipmapsDisabledRuleSettings settings)
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetRuleSettings(profile, settings);
            return profile;
        }

        private static UiTextureMipmapsDisabledRuleSettings CreateSettings(
            bool includeSpriteTextures,
            params string[] uiPathPrefixes)
        {
            var settings = ScriptableObject.CreateInstance<UiTextureMipmapsDisabledRuleSettings>();
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("includeSpriteTextures").boolValue = includeSpriteTextures;

            var prefixesProperty = serializedSettings.FindProperty("uiPathPrefixes");
            prefixesProperty.arraySize = uiPathPrefixes.Length;
            for (var index = 0; index < uiPathPrefixes.Length; index++)
            {
                prefixesProperty.GetArrayElementAtIndex(index).stringValue = uiPathPrefixes[index];
            }

            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static AssetContext ScanSingle(string assetPath)
        {
            return AssetScanner.Scan(new[] { assetPath }).Single();
        }

        private static void ConfigureTexture(
            string assetPath,
            TextureImporterType textureType,
            bool mipmapEnabled)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = textureType;
            importer.mipmapEnabled = mipmapEnabled;
            importer.SaveAndReimport();
        }

        private static void WritePngAsset(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Unable to determine the Unity project root.");
            }

            File.WriteAllBytes(
                Path.Combine(projectRoot, assetPath),
                Convert.FromBase64String(TestPngBase64));
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
