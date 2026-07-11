using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class ShortAudioStreamingRuleTests
    {
        private const string TestFolder = "Assets/__UnityAssetGovernanceShortAudioRuleTests";
        private const string ShortAudioFolder = TestFolder + "/SFX";
        private const string OtherAudioFolder = TestFolder + "/SFXBackup";
        private const string StreamingAudioPath = ShortAudioFolder + "/Streaming.wav";
        private const string NonStreamingAudioPath = ShortAudioFolder + "/NonStreaming.wav";
        private const string OutsideAudioPath = OtherAudioFolder + "/Outside.wav";
        private const string TextAssetPath = TestFolder + "/NotAudio.txt";

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();

            AssetDatabase.CreateFolder("Assets", "__UnityAssetGovernanceShortAudioRuleTests");
            AssetDatabase.CreateFolder(TestFolder, "SFX");
            AssetDatabase.CreateFolder(TestFolder, "SFXBackup");
            WriteSilentWav(StreamingAudioPath);
            WriteSilentWav(NonStreamingAudioPath);
            WriteSilentWav(OutsideAudioPath);
            File.WriteAllText(GetAbsolutePath(TextAssetPath), "Not audio.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureAudio(StreamingAudioPath, AudioClipLoadType.Streaming);
            ConfigureAudio(NonStreamingAudioPath, AudioClipLoadType.DecompressOnLoad);
            ConfigureAudio(OutsideAudioPath, AudioClipLoadType.Streaming);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestAssets();
        }

        [Test]
        public void CanEvaluate_RequiresConfiguredShortAudioPath()
        {
            var settings = CreateSettings(
                AudioClipLoadType.DecompressOnLoad,
                ShortAudioFolder);
            var profile = CreateProfile(settings);
            var rule = new ShortAudioStreamingRule();

            try
            {
                Assert.That(
                    rule.CanEvaluate(ScanSingle(StreamingAudioPath, profile)),
                    Is.True);
                Assert.That(
                    rule.CanEvaluate(ScanSingle(OutsideAudioPath, profile)),
                    Is.False);
                Assert.That(
                    rule.CanEvaluate(ScanSingle(TextAssetPath, profile)),
                    Is.False);
                Assert.That(
                    rule.CanEvaluate(ScanSingle(StreamingAudioPath)),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Evaluate_ReturnsNoIssueWhenShortAudioDoesNotUseStreaming()
        {
            var settings = CreateSettings(
                AudioClipLoadType.DecompressOnLoad,
                ShortAudioFolder);
            var profile = CreateProfile(settings);
            var rule = new ShortAudioStreamingRule();

            try
            {
                var issues = rule.Evaluate(ScanSingle(NonStreamingAudioPath, profile));

                Assert.That(issues, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void ScannerAndRunner_ReportFixableErrorForStreamingShortAudio()
        {
            var settings = CreateSettings(
                AudioClipLoadType.DecompressOnLoad,
                ShortAudioFolder);
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { StreamingAudioPath }, profile));

                var issue = result.Issues.Single(
                    candidate => candidate.RuleId == "UAG-AUDIO-001");
                Assert.That(issue.AssetPath, Is.EqualTo(StreamingAudioPath));
                Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Error));
                Assert.That(
                    issue.Message,
                    Is.EqualTo("Short audio clips must not use the Streaming Load Type."));
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
        public void Runner_ReportsConfigurationErrorWhenReplacementIsStreaming()
        {
            var settings = CreateSettings(
                AudioClipLoadType.Streaming,
                ShortAudioFolder);
            var profile = CreateProfile(settings);

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { StreamingAudioPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-AUDIO-001"),
                    Is.False);
                Assert.That(result.ExecutionErrors, Has.Count.EqualTo(1));
                Assert.That(result.ExecutionErrors[0].RuleId, Is.EqualTo("UAG-AUDIO-001"));
                Assert.That(
                    result.ExecutionErrors[0].Stage,
                    Is.EqualTo(RuleExecutionStage.CanEvaluate));
                Assert.That(
                    result.ExecutionErrors[0].Exception.Message,
                    Does.Contain("cannot be Streaming"));
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
            var settings = CreateSettings(
                AudioClipLoadType.DecompressOnLoad,
                ShortAudioFolder,
                ShortAudioFolder + "/");

            try
            {
                Assert.That(
                    () => settings.MatchesShortAudioPath(StreamingAudioPath),
                    Throws.InvalidOperationException.With.Message.Contains("duplicate path prefix"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_SkipsRuleForWhitelistedShortAudio()
        {
            var settings = CreateSettings(
                AudioClipLoadType.DecompressOnLoad,
                ShortAudioFolder);
            var profile = CreateProfile(settings);
            GovernanceProfileTests.SetWhitelistEntries(
                profile,
                (StreamingAudioPath, new[] { "UAG-AUDIO-001" }));

            try
            {
                var result = RuleRunner.Run(
                    AssetScanner.Scan(new[] { StreamingAudioPath }, profile));

                Assert.That(
                    result.Issues.Any(candidate => candidate.RuleId == "UAG-AUDIO-001"),
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
        public void FixRunner_AppliesConfiguredNonStreamingTypeAndIssueDisappears()
        {
            var settings = CreateSettings(
                AudioClipLoadType.CompressedInMemory,
                ShortAudioFolder);
            var profile = CreateProfile(settings);

            try
            {
                var context = ScanSingle(StreamingAudioPath, profile);
                var issue = RuleRunner.Run(new[] { context }).Issues
                    .Single(candidate => candidate.RuleId == "UAG-AUDIO-001");

                var fixResult = FixRunner.Fix(context, issue);
                var importer = (AudioImporter)AssetImporter.GetAtPath(StreamingAudioPath);
                var verificationResult = RuleRunner.Run(
                    AssetScanner.Scan(new[] { StreamingAudioPath }, profile));

                Assert.That(fixResult.Succeeded, Is.True);
                Assert.That(
                    importer.defaultSampleSettings.loadType,
                    Is.EqualTo(AudioClipLoadType.CompressedInMemory));
                Assert.That(
                    verificationResult.Issues.Any(
                        candidate => candidate.RuleId == "UAG-AUDIO-001"),
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
        public void DiscoverRules_FindsShortAudioRuleWithoutCentralRegistration()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == "UAG-AUDIO-001"),
                Is.EqualTo(1));
            Assert.That(
                rules.Single(rule => rule.Descriptor.Id == "UAG-AUDIO-001"),
                Is.TypeOf<ShortAudioStreamingRule>());
        }

        private static GovernanceProfile CreateProfile(ShortAudioStreamingRuleSettings settings)
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            GovernanceProfileTests.SetRuleSettings(profile, settings);
            return profile;
        }

        private static ShortAudioStreamingRuleSettings CreateSettings(
            AudioClipLoadType replacementLoadType,
            params string[] pathPrefixes)
        {
            var settings = ScriptableObject.CreateInstance<ShortAudioStreamingRuleSettings>();
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("replacementLoadType").enumValueIndex =
                (int)replacementLoadType;

            var prefixesProperty = serializedSettings.FindProperty("shortAudioPathPrefixes");
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
