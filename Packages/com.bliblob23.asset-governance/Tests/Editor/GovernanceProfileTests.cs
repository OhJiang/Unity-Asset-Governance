using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class GovernanceProfileTests
    {
        private const string TestFolder = "Assets/__UnityAssetGovernanceProfileTests";
        private const string ProfilePath = TestFolder + "/Profile.asset";

        private GovernanceProfile profile;
        private UiTextureMipmapsDisabledRuleSettings textureSettings;
        private TestRuleSettings otherSettings;

        [SetUp]
        public void SetUp()
        {
            DeleteTestAssets();
            AssetDatabase.CreateFolder("Assets", "__UnityAssetGovernanceProfileTests");

            profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            textureSettings = ScriptableObject.CreateInstance<UiTextureMipmapsDisabledRuleSettings>();
            otherSettings = ScriptableObject.CreateInstance<TestRuleSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            if (profile != null && !AssetDatabase.Contains(profile))
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }

            if (textureSettings != null)
            {
                UnityEngine.Object.DestroyImmediate(textureSettings);
            }

            if (otherSettings != null)
            {
                UnityEngine.Object.DestroyImmediate(otherSettings);
            }

            DeleteTestAssets();
        }

        [Test]
        public void IsRuleEnabled_ReturnsTrueWhenRuleHasNoState()
        {
            Assert.That(profile.IsRuleEnabled("UAG-NAME-001"), Is.True);
        }

        [Test]
        public void IsRuleEnabled_ReturnsConfiguredState()
        {
            SetRuleStates(profile, ("UAG-NAME-001", false));

            Assert.That(profile.IsRuleEnabled("UAG-NAME-001"), Is.False);
        }

        [Test]
        public void IsRuleEnabled_RejectsDuplicateRuleIds()
        {
            SetRuleStates(
                profile,
                ("UAG-NAME-001", true),
                ("UAG-NAME-001", false));

            var exception = Assert.Throws<InvalidOperationException>(() =>
                profile.IsRuleEnabled("UAG-NAME-001"));

            Assert.That(exception.Message, Does.Contain("duplicate states"));
        }

        [Test]
        public void IsRuleEnabled_RejectsEmptyRuleId()
        {
            SetRuleStates(profile, (string.Empty, true));

            var exception = Assert.Throws<InvalidOperationException>(() =>
                profile.IsRuleEnabled("UAG-NAME-001"));

            Assert.That(exception.Message, Does.Contain("empty rule ID"));
        }

        [Test]
        public void TryGetSeverityOverride_ReturnsFalseWhenRuleHasNoOverride()
        {
            SetRuleStates(profile, ("UAG-NAME-001", true));

            var found = profile.TryGetSeverityOverride(
                "UAG-NAME-001",
                out RuleSeverity severity);

            Assert.That(found, Is.False);
            Assert.That(severity, Is.EqualTo(default(RuleSeverity)));
        }

        [Test]
        public void TryGetSeverityOverride_ReturnsConfiguredSeverity()
        {
            SetRuleStatesWithSeverity(
                profile,
                ("UAG-NAME-001", true, true, RuleSeverity.Error));

            var found = profile.TryGetSeverityOverride(
                "UAG-NAME-001",
                out RuleSeverity severity);

            Assert.That(found, Is.True);
            Assert.That(severity, Is.EqualTo(RuleSeverity.Error));
        }

        [Test]
        public void TryGetSeverityOverride_RejectsInvalidSeverity()
        {
            SetRuleStatesWithSeverity(
                profile,
                ("UAG-NAME-001", true, true, (RuleSeverity)999));

            var exception = Assert.Throws<InvalidOperationException>(() =>
                profile.TryGetSeverityOverride(
                    "UAG-NAME-001",
                    out RuleSeverity _));

            Assert.That(exception.Message, Does.Contain("invalid severity"));
        }

        [Test]
        public void TryGetRuleSettings_ReturnsFalseWhenRuleHasNoSettings()
        {
            var found = profile.TryGetRuleSettings(
                "UAG-TEX-001",
                out UiTextureMipmapsDisabledRuleSettings settings);

            Assert.That(found, Is.False);
            Assert.That(settings, Is.Null);
        }

        [Test]
        public void TryGetRuleSettings_ReturnsMatchingStronglyTypedSettings()
        {
            SetRuleSettings(profile, textureSettings);

            var found = profile.TryGetRuleSettings(
                "UAG-TEX-001",
                out UiTextureMipmapsDisabledRuleSettings settings);

            Assert.That(found, Is.True);
            Assert.That(settings, Is.SameAs(textureSettings));
        }

        [Test]
        public void TryGetRuleSettings_RejectsDuplicateRuleIds()
        {
            var duplicateSettings = ScriptableObject.CreateInstance<UiTextureMipmapsDisabledRuleSettings>();
            try
            {
                SetRuleSettings(profile, textureSettings, duplicateSettings);

                var exception = Assert.Throws<InvalidOperationException>(() =>
                    profile.TryGetRuleSettings(
                        "UAG-TEX-001",
                        out UiTextureMipmapsDisabledRuleSettings _));

                Assert.That(exception.Message, Does.Contain("duplicate settings"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(duplicateSettings);
            }
        }

        [Test]
        public void TryGetRuleSettings_RejectsSettingsWithUnexpectedType()
        {
            SetRuleSettings(profile, otherSettings);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                profile.TryGetRuleSettings(
                    "UAG-TEST-SETTINGS",
                    out UiTextureMipmapsDisabledRuleSettings _));

            Assert.That(exception.Message, Does.Contain(typeof(UiTextureMipmapsDisabledRuleSettings).FullName));
            Assert.That(exception.Message, Does.Contain(typeof(TestRuleSettings).FullName));
        }

        [Test]
        public void LoadDefault_ReturnsNullWhenNoProfilePathIsProvided()
        {
            Assert.That(GovernanceProfileLocator.LoadDefault(Array.Empty<string>()), Is.Null);
        }

        [Test]
        public void LoadDefault_LoadsTheOnlyProvidedProfile()
        {
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();

            var loadedProfile = GovernanceProfileLocator.LoadDefault(new[] { ProfilePath });

            Assert.That(loadedProfile, Is.SameAs(profile));
        }

        [Test]
        public void LoadDefault_RejectsMultipleProfilePathsInDeterministicOrder()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                GovernanceProfileLocator.LoadDefault(new[]
                {
                    "Assets/ZProfile.asset",
                    "Assets/AProfile.asset"
                }));

            Assert.That(exception.Message, Does.Contain("Assets/AProfile.asset, Assets/ZProfile.asset"));
        }

        internal static void SetRuleStates(
            GovernanceProfile targetProfile,
            params (string RuleId, bool Enabled)[] states)
        {
            var extendedStates = new (
                string RuleId,
                bool Enabled,
                bool OverrideSeverity,
                RuleSeverity Severity)[states.Length];

            for (var index = 0; index < states.Length; index++)
            {
                extendedStates[index] = (
                    states[index].RuleId,
                    states[index].Enabled,
                    false,
                    RuleSeverity.Warning);
            }

            SetRuleStatesWithSeverity(targetProfile, extendedStates);
        }

        internal static void SetRuleStatesWithSeverity(
            GovernanceProfile targetProfile,
            params (
                string RuleId,
                bool Enabled,
                bool OverrideSeverity,
                RuleSeverity Severity)[] states)
        {
            var serializedProfile = new SerializedObject(targetProfile);
            var property = serializedProfile.FindProperty("ruleStates");
            property.arraySize = states.Length;

            for (var index = 0; index < states.Length; index++)
            {
                var element = property.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("ruleId").stringValue = states[index].RuleId;
                element.FindPropertyRelative("enabled").boolValue = states[index].Enabled;
                element.FindPropertyRelative("overrideSeverity").boolValue =
                    states[index].OverrideSeverity;
                element.FindPropertyRelative("severity").intValue = (int)states[index].Severity;
            }

            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetRuleSettings(
            GovernanceProfile targetProfile,
            params AssetRuleSettings[] settings)
        {
            var serializedProfile = new SerializedObject(targetProfile);
            var property = serializedProfile.FindProperty("ruleSettings");
            property.arraySize = settings.Length;

            for (var index = 0; index < settings.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = settings[index];
            }

            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DeleteTestAssets()
        {
            if (AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.DeleteAsset(TestFolder);
            }
        }
    }

    public sealed class TestRuleSettings : AssetRuleSettings
    {
        public override string RuleId => "UAG-TEST-SETTINGS";
    }
}
