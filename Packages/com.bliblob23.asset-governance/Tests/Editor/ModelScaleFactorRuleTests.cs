using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class ModelScaleFactorRuleTests
    {
        private const string TestFolder = "Assets/__UnityAssetGovernanceModelScaleFactorRuleTests";
        private const string ParentModelPath = TestFolder + "/ParentModel.obj";
        private const string CompliantModelPath = TestFolder + "/CompliantModel.obj";
        private const string HeroFolder = TestFolder + "/Characters/Hero";
        private const string HeroModelPath = HeroFolder + "/Hero.obj";
        private const string TextAssetPath = TestFolder + "/NotAModel.txt";
        private const string TestObj =
            "o Triangle\n" +
            "v 0 0 0\n" +
            "v 1 0 0\n" +
            "v 0 1 0\n" +
            "f 1 2 3\n";

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();

            AssetDatabase.CreateFolder("Assets", "__UnityAssetGovernanceModelScaleFactorRuleTests");
            AssetDatabase.CreateFolder(TestFolder, "Characters");
            AssetDatabase.CreateFolder(TestFolder + "/Characters", "Hero");
            WriteAsset(ParentModelPath, TestObj);
            WriteAsset(CompliantModelPath, TestObj);
            WriteAsset(HeroModelPath, TestObj);
            WriteAsset(TextAssetPath, "Not a model.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureModel(ParentModelPath, 2f);
            ConfigureModel(CompliantModelPath, 1f);
            ConfigureModel(HeroModelPath, 0.1f);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void CanEvaluate_AcceptsModelAndRejectsNonModelAsset()
        {
            var rule = new ModelScaleFactorRule();

            Assert.That(rule.CanEvaluate(ScanSingle(ParentModelPath)), Is.True);
            Assert.That(rule.CanEvaluate(ScanSingle(TextAssetPath)), Is.False);
        }

        [Test]
        public void Evaluate_ReturnsNoIssueWhenScaleFactorMatchesBuiltInDefault()
        {
            var rule = new ModelScaleFactorRule();

            var issues = rule.Evaluate(ScanSingle(CompliantModelPath));

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void ScannerAndRunner_ReportReadOnlyWarningWhenScaleFactorDiffers()
        {
            var result = RuleRunner.Run(AssetScanner.Scan(new[] { ParentModelPath }));

            var issue = result.Issues.Single(candidate => candidate.RuleId == "UAG-MODEL-001");
            Assert.That(issue.AssetPath, Is.EqualTo(ParentModelPath));
            Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Warning));
            Assert.That(
                issue.Message,
                Is.EqualTo("Model Scale Factor is 2, but the configured value is 1."));
            Assert.That(issue.CanFix, Is.False);
            Assert.That(result.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void Runner_UsesConfiguredDefaultScaleFactor()
        {
            var settings = CreateSettings(2f);
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { ParentModelPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-MODEL-001"),
                    Is.False);
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
                1f,
                (TestFolder, 0.01f),
                (HeroFolder, 0.1f));
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { HeroModelPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-MODEL-001"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_ReportsConfigurationErrorForInvalidScaleFactor()
        {
            var settings = CreateSettings(0f);
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { ParentModelPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-MODEL-001"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Has.Count.EqualTo(1));
                Assert.That(result.ExecutionErrors[0].RuleId, Is.EqualTo("UAG-MODEL-001"));
                Assert.That(
                    result.ExecutionErrors[0].Stage,
                    Is.EqualTo(RuleExecutionStage.CanEvaluate));
                Assert.That(
                    result.ExecutionErrors[0].Exception.Message,
                    Does.Contain("greater than zero"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Settings_RejectDuplicateNormalizedPathOverrides()
        {
            var settings = CreateSettings(
                1f,
                (TestFolder, 1f),
                (TestFolder + "/", 0.1f));

            try
            {
                Assert.That(
                    () => settings.GetExpectedScaleFactor(ParentModelPath),
                    Throws.InvalidOperationException.With.Message.Contains("duplicate path prefix"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_SkipsRuleForWhitelistedModel()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetWhitelistEntries(
                profile,
                (ParentModelPath, new[] { "UAG-MODEL-001" }));

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { ParentModelPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-MODEL-001"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void DiscoverRules_FindsModelScaleFactorRuleWithoutCentralRegistration()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == "UAG-MODEL-001"),
                Is.EqualTo(1));
            Assert.That(
                rules.Single(rule => rule.Descriptor.Id == "UAG-MODEL-001"),
                Is.TypeOf<ModelScaleFactorRule>());
        }

        private static GovernanceProfile CreateProfile(ModelScaleFactorRuleSettings settings)
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetRuleSettings(profile, settings);
            return profile;
        }

        private static ModelScaleFactorRuleSettings CreateSettings(
            float defaultExpectedScaleFactor,
            params (string PathPrefix, float ExpectedScaleFactor)[] pathOverrides)
        {
            var settings = ScriptableObject.CreateInstance<ModelScaleFactorRuleSettings>();
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("defaultExpectedScaleFactor").floatValue =
                defaultExpectedScaleFactor;

            var overridesProperty = serializedSettings.FindProperty("pathOverrides");
            overridesProperty.arraySize = pathOverrides.Length;
            for (var index = 0; index < pathOverrides.Length; index++)
            {
                var overrideProperty = overridesProperty.GetArrayElementAtIndex(index);
                overrideProperty.FindPropertyRelative("pathPrefix").stringValue =
                    pathOverrides[index].PathPrefix;
                overrideProperty.FindPropertyRelative("expectedScaleFactor").floatValue =
                    pathOverrides[index].ExpectedScaleFactor;
            }

            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static AssetContext ScanSingle(string assetPath)
        {
            return AssetScanner.Scan(new[] { assetPath }).Single();
        }

        private static void ConfigureModel(string assetPath, float scaleFactor)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(assetPath);
            importer.globalScale = scaleFactor;
            importer.SaveAndReimport();
        }

        private static void WriteAsset(string assetPath, string contents)
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
