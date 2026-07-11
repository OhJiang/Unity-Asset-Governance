using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class NormalMapSrgbDisabledRuleTests
    {
        private const string TestFolder = "Assets/__UnityAssetGovernanceNormalMapSrgbRuleTests";
        private const string SrgbNormalMapPath = TestFolder + "/SrgbNormal.png";
        private const string LinearNormalMapPath = TestFolder + "/LinearNormal.png";
        private const string DefaultTexturePath = TestFolder + "/DefaultTexture.png";
        private const string TextAssetPath = TestFolder + "/NotATexture.txt";
        private const string TestPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAYAAACp8Z5+AAAAD0lEQVR4nGP4jwYYSBcAAHg8P8FY7imoAAAAAElFTkSuQmCC";

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();

            AssetDatabase.CreateFolder("Assets", "__UnityAssetGovernanceNormalMapSrgbRuleTests");
            WritePngAsset(SrgbNormalMapPath);
            WritePngAsset(LinearNormalMapPath);
            WritePngAsset(DefaultTexturePath);
            File.WriteAllText(GetAbsolutePath(TextAssetPath), "Not a texture.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureTexture(SrgbNormalMapPath, TextureImporterType.NormalMap, true);
            ConfigureTexture(LinearNormalMapPath, TextureImporterType.NormalMap, false);
            ConfigureTexture(DefaultTexturePath, TextureImporterType.Default, true);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void CanEvaluate_OnlyAcceptsNormalMapTexture()
        {
            var rule = new NormalMapSrgbDisabledRule();

            Assert.That(rule.CanEvaluate(ScanSingle(SrgbNormalMapPath)), Is.True);
            Assert.That(rule.CanEvaluate(ScanSingle(DefaultTexturePath)), Is.False);
            Assert.That(rule.CanEvaluate(ScanSingle(TextAssetPath)), Is.False);
        }

        [Test]
        public void Evaluate_ReturnsNoIssueWhenNormalMapUsesLinearSampling()
        {
            var rule = new NormalMapSrgbDisabledRule();

            var issues = rule.Evaluate(ScanSingle(LinearNormalMapPath));

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void ScannerAndRunner_ReportFixableErrorWhenNormalMapUsesSrgbSampling()
        {
            var result = RuleRunner.Run(AssetScanner.Scan(new[] { SrgbNormalMapPath }));

            var issue = result.Issues.Single(candidate => candidate.RuleId == "UAG-TEX-004");
            Assert.That(issue.AssetPath, Is.EqualTo(SrgbNormalMapPath));
            Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Error));
            Assert.That(
                issue.Message,
                Is.EqualTo("Normal map sRGB texture sampling must be disabled."));
            Assert.That(issue.CanFix, Is.True);
            Assert.That(result.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void Runner_SkipsRuleForWhitelistedNormalMap()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetWhitelistEntries(
                profile,
                (SrgbNormalMapPath, new[] { "UAG-TEX-004" }));

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { SrgbNormalMapPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-TEX-004"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void FixRunner_DisablesSrgbAndIssueDisappearsAfterRescan()
        {
            var context = ScanSingle(SrgbNormalMapPath);
            var issue = RuleRunner.Run(new[] { context }).Issues
                .Single(candidate => candidate.RuleId == "UAG-TEX-004");

            var fixResult = FixRunner.Fix(context, issue);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SrgbNormalMapPath);
            var verificationResult = RuleRunner.Run(
                AssetScanner.Scan(new[] { SrgbNormalMapPath }));

            Assert.That(fixResult.Succeeded, Is.True);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.NormalMap));
            Assert.That(importer.sRGBTexture, Is.False);
            Assert.That(
                verificationResult.Issues.Any(candidate => candidate.RuleId == "UAG-TEX-004"),
                Is.False);
            Assert.That(verificationResult.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void DiscoverRules_FindsNormalMapSrgbRuleWithoutCentralRegistration()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == "UAG-TEX-004"),
                Is.EqualTo(1));
            Assert.That(
                rules.Single(rule => rule.Descriptor.Id == "UAG-TEX-004"),
                Is.TypeOf<NormalMapSrgbDisabledRule>());
        }

        private static AssetContext ScanSingle(string assetPath)
        {
            return AssetScanner.Scan(new[] { assetPath }).Single();
        }

        private static void ConfigureTexture(
            string assetPath,
            TextureImporterType textureType,
            bool sRgbTexture)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = textureType;
            importer.sRGBTexture = sRgbTexture;
            importer.SaveAndReimport();
        }

        private static void WritePngAsset(string assetPath)
        {
            File.WriteAllBytes(
                GetAbsolutePath(assetPath),
                Convert.FromBase64String(TestPngBase64));
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
