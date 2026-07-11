using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class ModelReadWriteDisabledRuleTests
    {
        private const string TestFolder = "Assets/__UnityAssetGovernanceModelReadWriteRuleTests";
        private const string ReadableModelPath = TestFolder + "/Readable.obj";
        private const string NonReadableModelPath = TestFolder + "/NonReadable.obj";
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

            AssetDatabase.CreateFolder("Assets", "__UnityAssetGovernanceModelReadWriteRuleTests");
            WriteAsset(ReadableModelPath, TestObj);
            WriteAsset(NonReadableModelPath, TestObj);
            WriteAsset(TextAssetPath, "Not a model.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureModel(ReadableModelPath, true);
            ConfigureModel(NonReadableModelPath, false);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void CanEvaluate_AcceptsModelAndRejectsNonModelAsset()
        {
            var rule = new ModelReadWriteDisabledRule();

            Assert.That(rule.CanEvaluate(ScanSingle(ReadableModelPath)), Is.True);
            Assert.That(rule.CanEvaluate(ScanSingle(TextAssetPath)), Is.False);
        }

        [Test]
        public void Evaluate_ReturnsNoIssueWhenReadWriteIsDisabled()
        {
            var rule = new ModelReadWriteDisabledRule();

            var issues = rule.Evaluate(ScanSingle(NonReadableModelPath));

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void ScannerAndRunner_ReportFixableWarningWhenReadWriteIsEnabled()
        {
            var result = RuleRunner.Run(AssetScanner.Scan(new[] { ReadableModelPath }));

            var issue = result.Issues.Single(candidate => candidate.RuleId == "UAG-MODEL-002");
            Assert.That(issue.AssetPath, Is.EqualTo(ReadableModelPath));
            Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Warning));
            Assert.That(
                issue.Message,
                Is.EqualTo(
                    "Model Read/Write must be disabled unless the asset is explicitly whitelisted."));
            Assert.That(issue.CanFix, Is.True);
            Assert.That(result.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void Runner_SkipsRuleForWhitelistedModel()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetWhitelistEntries(
                profile,
                (ReadableModelPath, new[] { "UAG-MODEL-002" }));

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { ReadableModelPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-MODEL-002"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void FixRunner_DisablesReadWriteAndIssueDisappearsAfterRescan()
        {
            var context = ScanSingle(ReadableModelPath);
            var issue = RuleRunner.Run(new[] { context }).Issues
                .Single(candidate => candidate.RuleId == "UAG-MODEL-002");

            var fixResult = FixRunner.Fix(context, issue);
            var importer = (ModelImporter)AssetImporter.GetAtPath(ReadableModelPath);
            var verificationResult = RuleRunner.Run(
                AssetScanner.Scan(new[] { ReadableModelPath }));

            Assert.That(fixResult.Succeeded, Is.True);
            Assert.That(importer.isReadable, Is.False);
            Assert.That(
                verificationResult.Issues.Any(candidate => candidate.RuleId == "UAG-MODEL-002"),
                Is.False);
            Assert.That(verificationResult.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void DiscoverRules_FindsModelReadWriteRuleWithoutCentralRegistration()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == "UAG-MODEL-002"),
                Is.EqualTo(1));
            Assert.That(
                rules.Single(rule => rule.Descriptor.Id == "UAG-MODEL-002"),
                Is.TypeOf<ModelReadWriteDisabledRule>());
        }

        private static AssetContext ScanSingle(string assetPath)
        {
            return AssetScanner.Scan(new[] { assetPath }).Single();
        }

        private static void ConfigureModel(string assetPath, bool isReadable)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(assetPath);
            importer.globalScale = ModelScaleFactorRuleSettings.DefaultExpectedScaleFactor;
            importer.isReadable = isReadable;
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
