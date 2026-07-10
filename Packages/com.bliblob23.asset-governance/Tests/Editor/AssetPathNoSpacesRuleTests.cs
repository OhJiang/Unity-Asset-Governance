using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class AssetPathNoSpacesRuleTests
    {
        private const string TestFolder = "Assets/__Unity Asset Governance Naming Rule Tests";
        private const string TestAssetPath = TestFolder + "/Valid.txt";

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();

            AssetDatabase.CreateFolder("Assets", "__Unity Asset Governance Naming Rule Tests");
            WriteTextAsset(TestAssetPath, "Naming rule integration test asset.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void Evaluate_ReturnsNoIssueWhenPathContainsNoSpaces()
        {
            var rule = new AssetPathNoSpacesRule();

            var issues = rule.Evaluate(CreateContext("Assets/Textures/Hero.png"));

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void Evaluate_ReturnsIssueWhenFileNameContainsSpace()
        {
            var rule = new AssetPathNoSpacesRule();

            var issue = rule.Evaluate(CreateContext("Assets/Textures/Hero Icon.png")).Single();

            AssertIssue(issue, "Assets/Textures/Hero Icon.png");
        }

        [Test]
        public void Evaluate_ReturnsIssueWhenParentFolderContainsSpace()
        {
            var rule = new AssetPathNoSpacesRule();

            var issue = rule.Evaluate(CreateContext("Assets/Character Textures/Hero.png")).Single();

            AssertIssue(issue, "Assets/Character Textures/Hero.png");
        }

        [Test]
        public void DiscoverRules_FindsNamingRuleWithoutCentralRegistration()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == "UAG-NAME-001"),
                Is.EqualTo(1));
            Assert.That(
                rules.Single(rule => rule.Descriptor.Id == "UAG-NAME-001"),
                Is.TypeOf<AssetPathNoSpacesRule>());
        }

        [Test]
        public void ScannerRegistryAndRunner_ProduceNamingIssue()
        {
            var contexts = AssetScanner.Scan(new[] { TestAssetPath });

            var result = RuleRunner.Run(contexts);

            Assert.That(result.ExecutionErrors, Is.Empty);
            var issue = result.Issues.Single(item => item.RuleId == "UAG-NAME-001");
            AssertIssue(issue, TestAssetPath);
        }

        private static AssetContext CreateContext(string assetPath)
        {
            return new AssetContext(
                "test-guid",
                assetPath,
                typeof(Texture2D),
                null,
                null,
                BuildTarget.StandaloneOSX);
        }

        private static void AssertIssue(ValidationIssue issue, string expectedAssetPath)
        {
            Assert.That(issue.RuleId, Is.EqualTo("UAG-NAME-001"));
            Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Warning));
            Assert.That(issue.AssetPath, Is.EqualTo(expectedAssetPath));
            Assert.That(issue.Message, Is.EqualTo("Asset path must not contain spaces."));
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
