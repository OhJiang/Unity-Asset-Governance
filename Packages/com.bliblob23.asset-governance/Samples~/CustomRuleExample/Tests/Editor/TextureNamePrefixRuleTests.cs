using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.CustomRuleExample.Tests
{
    public sealed class TextureNamePrefixRuleTests
    {
        [Test]
        public void Registry_DiscoversRuleFromIndependentAssembly()
        {
            var rules = RuleRegistry.DiscoverRules();

            Assert.That(
                rules.Count(rule => rule.Descriptor.Id == TextureNamePrefixRuleSettings.StableRuleId),
                Is.EqualTo(1));
        }

        [Test]
        public void Runner_SkipsRuleWhenCustomSettingsAreMissing()
        {
            var context = CreateContext("Assets/Art/Textures/Hero.png", null);

            var result = RuleRunner.Run(
                new[] { context },
                new IAssetRule[] { new TextureNamePrefixRule() });

            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void Runner_UsesThirdPartyStronglyTypedSettings()
        {
            var settings = CreateSettings("Assets/Art/Textures", "T_");
            var profile = CreateProfile(settings);

            try
            {
                var context = CreateContext("Assets/Art/Textures/Hero.png", profile);
                var result = RuleRunner.Run(
                    new[] { context },
                    new IAssetRule[] { new TextureNamePrefixRule() });

                var issue = result.Issues.Single();
                Assert.That(issue.RuleId, Is.EqualTo(TextureNamePrefixRuleSettings.StableRuleId));
                Assert.That(issue.AssetPath, Is.EqualTo("Assets/Art/Textures/Hero.png"));
                Assert.That(issue.Message, Is.EqualTo("Texture name must start with 'T_'."));
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Runner_AcceptsTextureUsingConfiguredPrefix()
        {
            var settings = CreateSettings("Assets/Art/Textures", "T_");
            var profile = CreateProfile(settings);

            try
            {
                var context = CreateContext("Assets/Art/Textures/T_Hero.png", profile);
                var result = RuleRunner.Run(
                    new[] { context },
                    new IAssetRule[] { new TextureNamePrefixRule() });

                Assert.That(result.Issues, Is.Empty);
                Assert.That(result.ExecutionErrors, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(settings);
            }
        }

        private static TextureNamePrefixRuleSettings CreateSettings(
            string assetPathPrefix,
            string requiredNamePrefix)
        {
            var settings = ScriptableObject.CreateInstance<TextureNamePrefixRuleSettings>();
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("assetPathPrefix").stringValue = assetPathPrefix;
            serializedSettings.FindProperty("requiredNamePrefix").stringValue = requiredNamePrefix;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static GovernanceProfile CreateProfile(TextureNamePrefixRuleSettings settings)
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            var serializedProfile = new SerializedObject(profile);
            var ruleSettings = serializedProfile.FindProperty("ruleSettings");
            ruleSettings.arraySize = 1;
            ruleSettings.GetArrayElementAtIndex(0).objectReferenceValue = settings;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static AssetContext CreateContext(
            string assetPath,
            GovernanceProfile profile)
        {
            return new AssetContext(
                "sample-guid",
                assetPath,
                typeof(Texture2D),
                null,
                null,
                BuildTarget.StandaloneOSX,
                profile);
        }
    }
}
