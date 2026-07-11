using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class AssetPathForbiddenCharactersRuleTests
    {
        private const string TestFolder =
            "Assets/__UnityAssetGovernanceForbiddenCharactersRuleTests";
        private const string ChineseAssetPath = TestFolder + "/角色.txt";
        private const string ConfiguredForbiddenAssetPath = TestFolder + "/Hero@2x.txt";
        private const string CompliantAssetPath = TestFolder + "/HeroIcon.txt";

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();

            AssetDatabase.CreateFolder(
                "Assets",
                "__UnityAssetGovernanceForbiddenCharactersRuleTests");
            WriteTextAsset(ChineseAssetPath, "Chinese path integration test.");
            WriteTextAsset(ConfiguredForbiddenAssetPath, "Forbidden path integration test.");
            WriteTextAsset(CompliantAssetPath, "Compliant path integration test.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void Evaluate_ReturnsNoIssueForCompliantPathWithoutSettings()
        {
            var rule = new AssetPathForbiddenCharactersRule();

            var issues = rule.Evaluate(CreateContext("Assets/Textures/HeroIcon.png"));

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void ScannerAndRunner_ReportReadOnlyWarningForChinesePath()
        {
            var result = RuleRunner.Run(AssetScanner.Scan(new[] { ChineseAssetPath }));

            var issue = result.Issues.Single(candidate => candidate.RuleId == "UAG-NAME-002");
            Assert.That(issue.AssetPath, Is.EqualTo(ChineseAssetPath));
            Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Warning));
            Assert.That(
                issue.Message,
                Is.EqualTo(
                    "Asset path contains Chinese or configured forbidden characters: '角', '色'."));
            Assert.That(issue.CanFix, Is.False);
            Assert.That(result.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void Evaluate_DetectsSupplementaryChineseCodePoint()
        {
            var rule = new AssetPathForbiddenCharactersRule();

            var issue = rule.Evaluate(CreateContext("Assets/Models/𠀀.obj")).Single();

            Assert.That(
                issue.Message,
                Is.EqualTo(
                    "Asset path contains Chinese or configured forbidden characters: '𠀀'."));
        }

        [Test]
        public void Runner_ReportsConfiguredForbiddenCharacter()
        {
            var settings = CreateSettings("@#");
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { ConfiguredForbiddenAssetPath }, profile));

                var issue = result.Issues.Single(
                    candidate => candidate.RuleId == "UAG-NAME-002");
                Assert.That(
                    issue.Message,
                    Is.EqualTo(
                        "Asset path contains Chinese or configured forbidden characters: '@'."));
                Assert.That(issue.CanFix, Is.False);
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_DoesNotTreatProjectCharacterAsForbiddenWithoutSettings()
        {
            var result = RuleRunner.Run(
                AssetScanner.Scan(new[] { ConfiguredForbiddenAssetPath }));

            Assert.That(
                result.Issues.Any(candidate => candidate.RuleId == "UAG-NAME-002"),
                Is.False);
            Assert.That(result.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void Runner_ReportsConfigurationErrorForStructuralCharacter()
        {
            var settings = CreateSettings(".");
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { CompliantAssetPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-NAME-002"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Has.Count.EqualTo(1));
                Assert.That(result.ExecutionErrors[0].RuleId, Is.EqualTo("UAG-NAME-002"));
                Assert.That(
                    result.ExecutionErrors[0].Stage,
                    Is.EqualTo(RuleExecutionStage.CanEvaluate));
                Assert.That(
                    result.ExecutionErrors[0].Exception.Message,
                    Does.Contain("path separators or the file extension separator"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Settings_RejectDuplicateForbiddenCharacters()
        {
            var settings = CreateSettings("@@");

            try
            {
                Assert.That(
                    () => settings.GetForbiddenCodePoints(),
                    Throws.InvalidOperationException.With.Message.Contains(
                        "duplicate forbidden character"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_SkipsRuleForWhitelistedAsset()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetWhitelistEntries(
                profile,
                (ChineseAssetPath, new[] { "UAG-NAME-002" }));

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { ChineseAssetPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-NAME-002"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void DiscoverRules_FindsForbiddenCharactersRuleWithoutCentralRegistration()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == "UAG-NAME-002"),
                Is.EqualTo(1));
            Assert.That(
                rules.Single(rule => rule.Descriptor.Id == "UAG-NAME-002"),
                Is.TypeOf<AssetPathForbiddenCharactersRule>());
        }

        private static GovernanceProfile CreateProfile(
            AssetPathForbiddenCharactersRuleSettings settings)
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetRuleSettings(profile, settings);
            return profile;
        }

        private static AssetPathForbiddenCharactersRuleSettings CreateSettings(
            string forbiddenCharacters)
        {
            var settings =
                ScriptableObject.CreateInstance<AssetPathForbiddenCharactersRuleSettings>();
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("forbiddenCharacters").stringValue =
                forbiddenCharacters;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static AssetContext CreateContext(string assetPath)
        {
            return new AssetContext(
                "test-guid",
                assetPath,
                typeof(TextAsset),
                null,
                null,
                BuildTarget.StandaloneOSX);
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
