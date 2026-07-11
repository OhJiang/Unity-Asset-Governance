using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class LongAudioStreamingRuleTests
    {
        private const string TestFolder = "Assets/__UnityAssetGovernanceLongAudioRuleTests";
        private const string LongAudioFolder = TestFolder + "/BGM";
        private const string OtherAudioFolder = TestFolder + "/BGMBackup";
        private const string StreamingAudioPath = LongAudioFolder + "/Streaming.wav";
        private const string NonStreamingAudioPath = LongAudioFolder + "/NonStreaming.wav";
        private const string OutsideAudioPath = OtherAudioFolder + "/Outside.wav";
        private const string TextAssetPath = TestFolder + "/NotAudio.txt";

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();

            AssetDatabase.CreateFolder("Assets", "__UnityAssetGovernanceLongAudioRuleTests");
            AssetDatabase.CreateFolder(TestFolder, "BGM");
            AssetDatabase.CreateFolder(TestFolder, "BGMBackup");
            WriteSilentWav(StreamingAudioPath);
            WriteSilentWav(NonStreamingAudioPath);
            WriteSilentWav(OutsideAudioPath);
            File.WriteAllText(GetAbsolutePath(TextAssetPath), "Not audio.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureAudio(StreamingAudioPath, AudioClipLoadType.Streaming);
            ConfigureAudio(NonStreamingAudioPath, AudioClipLoadType.CompressedInMemory);
            ConfigureAudio(OutsideAudioPath, AudioClipLoadType.CompressedInMemory);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void CanEvaluate_RequiresConfiguredLongAudioPath()
        {
            var settings = CreateSettings(LongAudioFolder);
            var profile = CreateProfile(settings);
            var rule = new LongAudioStreamingRule();

            try
            {
                Assert.That(
                    rule.CanEvaluate(ScanSingle(NonStreamingAudioPath, profile)),
                    Is.True);
                Assert.That(
                    rule.CanEvaluate(ScanSingle(OutsideAudioPath, profile)),
                    Is.False);
                Assert.That(
                    rule.CanEvaluate(ScanSingle(TextAssetPath, profile)),
                    Is.False);
                Assert.That(
                    rule.CanEvaluate(ScanSingle(NonStreamingAudioPath)),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Evaluate_ReturnsNoIssueWhenLongAudioUsesStreaming()
        {
            var settings = CreateSettings(LongAudioFolder);
            var profile = CreateProfile(settings);
            var rule = new LongAudioStreamingRule();

            try
            {
                var issues = rule.Evaluate(ScanSingle(StreamingAudioPath, profile));

                Assert.That(issues, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void ScannerAndRunner_ReportFixableWarningForNonStreamingLongAudio()
        {
            var settings = CreateSettings(LongAudioFolder);
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { NonStreamingAudioPath }, profile));

                var issue = result.Issues.Single(
                    candidate => candidate.RuleId == "UAG-AUDIO-002");
                Assert.That(issue.AssetPath, Is.EqualTo(NonStreamingAudioPath));
                Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Warning));
                Assert.That(
                    issue.Message,
                    Is.EqualTo("Long audio clips must use the Streaming Load Type."));
                Assert.That(issue.CanFix, Is.True);
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_ReportsConfigurationErrorForInvalidProjectPath()
        {
            var settings = CreateSettings("Audio/BGM");
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { NonStreamingAudioPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-AUDIO-002"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Has.Count.EqualTo(1));
                Assert.That(result.ExecutionErrors[0].RuleId, Is.EqualTo("UAG-AUDIO-002"));
                Assert.That(
                    result.ExecutionErrors[0].Stage,
                    Is.EqualTo(RuleExecutionStage.CanEvaluate));
                Assert.That(
                    result.ExecutionErrors[0].Exception.Message,
                    Does.Contain("must start with 'Assets' or 'Packages'"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Settings_RejectDuplicateNormalizedPathPrefixes()
        {
            var settings = CreateSettings(LongAudioFolder, LongAudioFolder + "/");

            try
            {
                Assert.That(
                    () => settings.MatchesLongAudioPath(NonStreamingAudioPath),
                    Throws.InvalidOperationException.With.Message.Contains("duplicate path prefix"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_SkipsRuleForWhitelistedLongAudio()
        {
            var settings = CreateSettings(LongAudioFolder);
            var profile = CreateProfile(settings);
            GovernanceProfileTests.SetWhitelistEntries(
                profile,
                (NonStreamingAudioPath, new[] { "UAG-AUDIO-002" }));

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { NonStreamingAudioPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-AUDIO-002"),
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
        public void FixRunner_EnablesStreamingAndIssueDisappearsAfterRescan()
        {
            var settings = CreateSettings(LongAudioFolder);
            var profile = CreateProfile(settings);

            try
            {
                var context = ScanSingle(NonStreamingAudioPath, profile);
                var issue = RuleRunner.Run(new[] { context }).Issues
                    .Single(candidate => candidate.RuleId == "UAG-AUDIO-002");

                var fixResult = FixRunner.Fix(context, issue);
                var importer = (AudioImporter)AssetImporter.GetAtPath(NonStreamingAudioPath);
                var verificationResult = RuleRunner.Run(
                    AssetScanner.Scan(new[] { NonStreamingAudioPath }, profile));

                Assert.That(fixResult.Succeeded, Is.True);
                Assert.That(
                    importer.defaultSampleSettings.loadType,
                    Is.EqualTo(AudioClipLoadType.Streaming));
                Assert.That(
                    verificationResult.Issues.Any(
                        candidate => candidate.RuleId == "UAG-AUDIO-002"),
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
        public void DiscoverRules_FindsLongAudioRuleWithoutCentralRegistration()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == "UAG-AUDIO-002"),
                Is.EqualTo(1));
            Assert.That(
                rules.Single(rule => rule.Descriptor.Id == "UAG-AUDIO-002"),
                Is.TypeOf<LongAudioStreamingRule>());
        }

        private static GovernanceProfile CreateProfile(LongAudioStreamingRuleSettings settings)
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetRuleSettings(profile, settings);
            return profile;
        }

        private static LongAudioStreamingRuleSettings CreateSettings(params string[] pathPrefixes)
        {
            var settings = ScriptableObject.CreateInstance<LongAudioStreamingRuleSettings>();
            var serializedSettings = new SerializedObject(settings);
            var prefixesProperty = serializedSettings.FindProperty("longAudioPathPrefixes");
            prefixesProperty.arraySize = pathPrefixes.Length;
            for (var index = 0; index < pathPrefixes.Length; index++)
            {
                prefixesProperty.GetArrayElementAtIndex(index).stringValue = pathPrefixes[index];
            }

            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static AssetContext ScanSingle(
            string assetPath,
            GovernanceProfile profile = null)
        {
            return AssetScanner.Scan(new[] { assetPath }, profile).Single();
        }

        private static void ConfigureAudio(string assetPath, AudioClipLoadType loadType)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(assetPath);
            var sampleSettings = importer.defaultSampleSettings;
            sampleSettings.loadType = loadType;
            importer.defaultSampleSettings = sampleSettings;
            importer.SaveAndReimport();
        }

        private static void WriteSilentWav(string assetPath)
        {
            const int sampleRate = 8000;
            const short channelCount = 1;
            const short bitsPerSample = 16;
            const int sampleCount = 800;
            var dataSize = sampleCount * channelCount * bitsPerSample / 8;

            using (var stream = File.Create(GetAbsolutePath(assetPath)))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(channelCount);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channelCount * bitsPerSample / 8);
                writer.Write((short)(channelCount * bitsPerSample / 8));
                writer.Write(bitsPerSample);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);
                writer.Write(new byte[dataSize]);
            }
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
