using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class RuleModelTests
    {
        [Test]
        public void RuleDescriptor_CapturesStableMetadataAndAssetTypes()
        {
            var assetTypes = new List<Type> { typeof(Texture2D) };
            var descriptor = new RuleDescriptor(
                "naming.no-spaces",
                "No Spaces In File Names",
                "Asset file names should not contain spaces.",
                RuleSeverity.Warning,
                assetTypes);

            assetTypes.Clear();

            Assert.That(descriptor.Id, Is.EqualTo("naming.no-spaces"));
            Assert.That(descriptor.DisplayName, Is.EqualTo("No Spaces In File Names"));
            Assert.That(descriptor.DefaultSeverity, Is.EqualTo(RuleSeverity.Warning));
            Assert.That(descriptor.ApplicableAssetTypes, Is.EqualTo(new[] { typeof(Texture2D) }));
        }

        [Test]
        public void AssetContext_ExposesScannerDataWithoutRequiringLoadedObjects()
        {
            var context = new AssetContext(
                "asset-guid",
                "Assets/Textures/Hero.png",
                typeof(Texture2D),
                null,
                null,
                BuildTarget.Android);

            Assert.That(context.AssetGuid, Is.EqualTo("asset-guid"));
            Assert.That(context.AssetPath, Is.EqualTo("Assets/Textures/Hero.png"));
            Assert.That(context.AssetType, Is.EqualTo(typeof(Texture2D)));
            Assert.That(context.Asset, Is.Null);
            Assert.That(context.Importer, Is.Null);
            Assert.That(context.BuildTarget, Is.EqualTo(BuildTarget.Android));
        }

        [Test]
        public void ValidationIssue_CapturesRuleResult()
        {
            var issue = new ValidationIssue(
                "naming.no-spaces",
                RuleSeverity.Warning,
                "Assets/My Texture.png",
                "Asset file name contains a space.");

            Assert.That(issue.RuleId, Is.EqualTo("naming.no-spaces"));
            Assert.That(issue.Severity, Is.EqualTo(RuleSeverity.Warning));
            Assert.That(issue.AssetPath, Is.EqualTo("Assets/My Texture.png"));
            Assert.That(issue.Message, Is.EqualTo("Asset file name contains a space."));
        }

        [Test]
        public void CustomRule_CanImplementAndUsePublicRuleContract()
        {
            IAssetRule rule = new ExampleRule<TextureRuleMarker>();
            var context = new AssetContext(
                "asset-guid",
                "Assets/My Texture.png",
                typeof(Texture2D),
                null,
                null,
                BuildTarget.StandaloneOSX);

            Assert.That(rule.CanEvaluate(context), Is.True);

            var issue = rule.Evaluate(context).Single();
            Assert.That(issue.RuleId, Is.EqualTo(rule.Descriptor.Id));
            Assert.That(issue.AssetPath, Is.EqualTo(context.AssetPath));
        }

        private sealed class ExampleRule<TMarker> : IAssetRule
        {
            private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
                "example.rule",
                "Example Rule",
                "Used to verify the public extension contract.",
                RuleSeverity.Info,
                new[] { typeof(Texture2D) });

            public RuleDescriptor Descriptor => DescriptorValue;

            public bool CanEvaluate(AssetContext context)
            {
                return context.AssetType == typeof(Texture2D);
            }

            public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
            {
                yield return new ValidationIssue(
                    Descriptor.Id,
                    Descriptor.DefaultSeverity,
                    context.AssetPath,
                    "Example issue.");
            }
        }

        private sealed class TextureRuleMarker
        {
        }
    }
}
