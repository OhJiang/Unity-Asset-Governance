using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class TextureReadWriteDisabledRuleTests
    {
        private const string TestFolder = "Assets/__UnityAssetGovernanceTextureReadWriteRuleTests";
        private const string ReadableTexturePath = TestFolder + "/Readable.png";
        private const string NonReadableTexturePath = TestFolder + "/NonReadable.png";
        private const string TextAssetPath = TestFolder + "/NotATexture.txt";
        private const string TestPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAYAAACp8Z5+AAAAD0lEQVR4nGP4jwYYSBcAAHg8P8FY7imoAAAAAElFTkSuQmCC";

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();

            AssetDatabase.CreateFolder("Assets", "__UnityAssetGovernanceTextureReadWriteRuleTests");
            WritePngAsset(ReadableTexturePath);
            WritePngAsset(NonReadableTexturePath);
            WriteTextAsset(TextAssetPath, "Not a texture.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureTexture(ReadableTexturePath, true);
            ConfigureTexture(NonReadableTexturePath, false);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void CanEvaluate_AcceptsTextureAndRejectsNonTextureAsset()
        {
            var rule = new TextureReadWriteDisabledRule();
            var textureContext = ScanSingle(ReadableTexturePath);
            var textContext = ScanSingle(TextAssetPath);

            Assert.That(rule.CanEvaluate(textureContext), Is.True);
            Assert.That(rule.CanEvaluate(textContext), Is.False);
        }

        [Test]
        public void Evaluate_ReturnsNoIssueWhenReadWriteIsDisabled()
        {
            var rule = new TextureReadWriteDisabledRule();
            var context = ScanSingle(NonReadableTexturePath);

            var issues = rule.Evaluate(context);

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void ScannerAndRunner_ReportFixableIssueWhenReadWriteIsEnabled()
        {
            var result = RuleRunner.Run(AssetScanner.Scan(new[] { ReadableTexturePath }));

            var issue = result.Issues.Single(candidate => candidate.RuleId == "UAG-TEX-002");
            Assert.That(issue.AssetPath, Is.EqualTo(ReadableTexturePath));
            Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Warning));
            Assert.That(
                issue.Message,
                Is.EqualTo(
                    "Texture Read/Write must be disabled unless the asset is explicitly whitelisted."));
            Assert.That(issue.CanFix, Is.True);
            Assert.That(result.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void Runner_SkipsRuleForWhitelistedTexture()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetWhitelistEntries(
                profile,
                (ReadableTexturePath, new[] { "UAG-TEX-002" }));

            try
            {
                var contexts = AssetScanner.Scan(new[] { ReadableTexturePath }, profile);
                var result = RuleRunner.Run(contexts);

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-TEX-002"),
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
            var context = ScanSingle(ReadableTexturePath);
            var issue = RuleRunner.Run(new[] { context }).Issues
                .Single(candidate => candidate.RuleId == "UAG-TEX-002");

            var fixResult = FixRunner.Fix(context, issue);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ReadableTexturePath);
            var verificationResult = RuleRunner.Run(
                AssetScanner.Scan(new[] { ReadableTexturePath }));

            Assert.That(fixResult.Succeeded, Is.True);
            Assert.That(importer.isReadable, Is.False);
            Assert.That(
                verificationResult.Issues.Any(candidate => candidate.RuleId == "UAG-TEX-002"),
                Is.False);
            Assert.That(verificationResult.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void DiscoverRules_FindsReadWriteRuleWithoutCentralRegistration()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == "UAG-TEX-002"),
                Is.EqualTo(1));
            Assert.That(
                rules.Single(rule => rule.Descriptor.Id == "UAG-TEX-002"),
                Is.TypeOf<TextureReadWriteDisabledRule>());
        }

        private static AssetContext ScanSingle(string assetPath)
        {
            return AssetScanner.Scan(new[] { assetPath }).Single();
        }

        private static void ConfigureTexture(string assetPath, bool isReadable)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = isReadable;
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
