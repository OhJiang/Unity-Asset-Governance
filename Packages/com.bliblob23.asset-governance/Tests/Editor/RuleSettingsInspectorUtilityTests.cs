using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityAssetGovernance.Tests
{
    public sealed class RuleSettingsInspectorUtilityTests
    {
        private string _testFolder;

        [SetUp]
        public void SetUp()
        {
            _testFolder = $"Assets/__RuleSettingsInspectorUtilityTests_{Guid.NewGuid():N}";
            AssetDatabase.CreateFolder("Assets", _testFolder.Substring("Assets/".Length));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(_testFolder);
        }

        [Test]
        public void CreateOptions_IncludesExtensibleSettingsAndSortsByRuleId()
        {
            var ruleOptions = new[]
            {
                new GovernanceProfileEditor.RuleOption("TEST-SETTINGS-A", "TEST-SETTINGS-A — Rule A"),
                new GovernanceProfileEditor.RuleOption("TEST-SETTINGS-B", "TEST-SETTINGS-B — Rule B")
            };

            var options = RuleSettingsInspectorUtility.CreateOptions(
                new[] { typeof(EditorTestRuleSettingsB), typeof(EditorTestRuleSettingsA) },
                ruleOptions);

            Assert.That(
                options.Select(option => option.RuleId),
                Is.EqualTo(new[] { "TEST-SETTINGS-A", "TEST-SETTINGS-B" }));
            Assert.That(options[0].Label, Is.EqualTo("TEST-SETTINGS-A — Rule A"));
            Assert.That(options[0].FileName, Is.EqualTo("EditorTestRuleSettingsA"));
        }

        [Test]
        public void CreateOptions_SkipsAbstractAndUnmarkedSettingsTypes()
        {
            var options = RuleSettingsInspectorUtility.CreateOptions(
                new[]
                {
                    typeof(AbstractEditorTestRuleSettings),
                    typeof(UnmarkedEditorTestRuleSettings),
                    typeof(EditorTestRuleSettingsA)
                },
                Array.Empty<GovernanceProfileEditor.RuleOption>());

            Assert.That(options.Select(option => option.SettingsType),
                Is.EqualTo(new[] { typeof(EditorTestRuleSettingsA) }));
        }

        [Test]
        public void DiscoverOptions_ExcludesTestAssembliesAndIncludesBuiltInSettings()
        {
            var options = RuleSettingsInspectorUtility.DiscoverOptions(
                Array.Empty<GovernanceProfileEditor.RuleOption>());

            Assert.That(
                options.Select(option => option.SettingsType),
                Does.Contain(typeof(TextureMaxSizeRuleSettings)));
            Assert.That(
                options.Any(option =>
                    option.SettingsType.Assembly.GetName().Name.EndsWith(
                        ".Tests",
                        StringComparison.Ordinal)),
                Is.False);
        }

        [Test]
        public void TryAddReference_RejectsSecondSettingsForSameRuleId()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            var firstSettings = CreateSettingsAsset<EditorTestRuleSettingsA>("FirstSettings.asset");
            var duplicateSettings = CreateSettingsAsset<DuplicateEditorTestRuleSettingsA>(
                "DuplicateSettings.asset");
            var serializedProfile = new SerializedObject(profile);
            var ruleSettings = serializedProfile.FindProperty("ruleSettings");

            try
            {
                Assert.That(
                    RuleSettingsInspectorUtility.TryAddReference(
                        ruleSettings,
                        firstSettings,
                        out var firstError),
                    Is.True,
                    firstError);
                serializedProfile.ApplyModifiedProperties();
                serializedProfile.Update();

                Assert.That(
                    RuleSettingsInspectorUtility.TryAddReference(
                        ruleSettings,
                        duplicateSettings,
                        out var duplicateError),
                    Is.False);
                Assert.That(duplicateError, Does.Contain("already has Rule Settings"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void CreateAndAttach_CreatesAssetBesideProfileAndAddsReference()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            var profilePath = $"{_testFolder}/GovernanceProfile.asset";
            AssetDatabase.CreateAsset(profile, profilePath);
            var serializedProfile = new SerializedObject(profile);
            var ruleSettings = serializedProfile.FindProperty("ruleSettings");
            var option = RuleSettingsInspectorUtility.CreateOptions(
                new[] { typeof(EditorTestRuleSettingsA) },
                Array.Empty<GovernanceProfileEditor.RuleOption>())[0];

            var created = RuleSettingsInspectorUtility.CreateAndAttach(
                profile,
                serializedProfile,
                ruleSettings,
                option);

            Assert.That(profile.RuleSettings, Has.Count.EqualTo(1));
            Assert.That(profile.RuleSettings[0], Is.SameAs(created));
            Assert.That(
                AssetDatabase.GetAssetPath(created),
                Does.StartWith($"{_testFolder}/Rule Settings/EditorTestRuleSettingsA"));
            Assert.That(AssetDatabase.Contains(created), Is.True);
        }

        private TSettings CreateSettingsAsset<TSettings>(string fileName)
            where TSettings : AssetRuleSettings
        {
            var settings = ScriptableObject.CreateInstance<TSettings>();
            AssetDatabase.CreateAsset(settings, $"{_testFolder}/{fileName}");
            return settings;
        }
    }

    [CreateAssetMenu(fileName = "EditorTestRuleSettingsA")]
    public sealed class EditorTestRuleSettingsA : AssetRuleSettings
    {
        public override string RuleId => "TEST-SETTINGS-A";
    }

    [CreateAssetMenu(fileName = "EditorTestRuleSettingsB")]
    public sealed class EditorTestRuleSettingsB : AssetRuleSettings
    {
        public override string RuleId => "TEST-SETTINGS-B";
    }

    [CreateAssetMenu(fileName = "DuplicateEditorTestRuleSettingsA")]
    public sealed class DuplicateEditorTestRuleSettingsA : AssetRuleSettings
    {
        public override string RuleId => "TEST-SETTINGS-A";
    }

    public sealed class UnmarkedEditorTestRuleSettings : AssetRuleSettings
    {
        public override string RuleId => "TEST-SETTINGS-UNMARKED";
    }

    public abstract class AbstractEditorTestRuleSettings : AssetRuleSettings
    {
    }
}
