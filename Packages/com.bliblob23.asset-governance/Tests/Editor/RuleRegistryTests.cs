using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnityAssetGovernance.Tests
{
    public sealed class RuleRegistryTests
    {
        [Test]
        public void DiscoverRules_UsesTypeCacheWithoutCentralRegistration()
        {
            Assert.That(() => RuleRegistry.DiscoverRules(), Throws.Nothing);
        }

        [Test]
        public void DiscoverRules_InstantiatesConcreteRules()
        {
            var rules = RuleRegistry.DiscoverRules(new[]
            {
                typeof(ValidRule<AlphaRuleMarker>)
            });

            Assert.That(rules, Has.Count.EqualTo(1));
            Assert.That(rules[0], Is.TypeOf<ValidRule<AlphaRuleMarker>>());
        }

        [Test]
        public void DiscoverRules_IgnoresAbstractAndOpenGenericRules()
        {
            var rules = RuleRegistry.DiscoverRules(new[]
            {
                typeof(AbstractRule<AlphaRuleMarker>),
                typeof(ValidRule<>),
                typeof(ValidRule<AlphaRuleMarker>)
            });

            Assert.That(rules, Has.Count.EqualTo(1));
            Assert.That(rules[0], Is.TypeOf<ValidRule<AlphaRuleMarker>>());
        }

        [Test]
        public void DiscoverRules_RejectsRuleWithoutPublicParameterlessConstructor()
        {
            Assert.That(
                () => RuleRegistry.DiscoverRules(new[]
                {
                    typeof(RuleWithoutParameterlessConstructor<AlphaRuleMarker>)
                }),
                Throws.InvalidOperationException
                    .With.Message.Contains("public parameterless constructor"));
        }

        [Test]
        public void DiscoverRules_ReportsConstructorFailure()
        {
            Assert.That(
                () => RuleRegistry.DiscoverRules(new[]
                {
                    typeof(ThrowingConstructorRule<AlphaRuleMarker>)
                }),
                Throws.InvalidOperationException
                    .With.Message.Contains(typeof(ThrowingConstructorRule<AlphaRuleMarker>).FullName));
        }

        [Test]
        public void DiscoverRules_RejectsNullDescriptor()
        {
            Assert.That(
                () => RuleRegistry.DiscoverRules(new[]
                {
                    typeof(NullDescriptorRule<AlphaRuleMarker>)
                }),
                Throws.InvalidOperationException
                    .With.Message.Contains("null descriptor"));
        }

        [Test]
        public void DiscoverRules_RejectsDuplicateRuleIds()
        {
            Assert.That(
                () => RuleRegistry.DiscoverRules(new[]
                {
                    typeof(DuplicateRule<AlphaRuleMarker>),
                    typeof(DuplicateRule<ZuluRuleMarker>)
                }),
                Throws.InvalidOperationException
                    .With.Message.Contains("test.duplicate")
                    .And.Message.Contains(typeof(DuplicateRule<AlphaRuleMarker>).FullName)
                    .And.Message.Contains(typeof(DuplicateRule<ZuluRuleMarker>).FullName));
        }

        [Test]
        public void DiscoverRules_SortsRulesByIdUsingOrdinalComparison()
        {
            var rules = RuleRegistry.DiscoverRules(new[]
            {
                typeof(ValidRule<ZuluRuleMarker>),
                typeof(ValidRule<AlphaRuleMarker>)
            });

            Assert.That(
                rules.Select(rule => rule.Descriptor.Id),
                Is.EqualTo(new[] { "test.AlphaRuleMarker", "test.ZuluRuleMarker" }));
        }

        public sealed class AlphaRuleMarker
        {
        }

        public sealed class ZuluRuleMarker
        {
        }

        public sealed class ValidRule<TMarker> : IAssetRule
        {
            private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
                $"test.{typeof(TMarker).Name}",
                "Valid Test Rule",
                "Used to verify rule discovery.",
                RuleSeverity.Info);

            public RuleDescriptor Descriptor => DescriptorValue;

            public bool CanEvaluate(AssetContext context)
            {
                return true;
            }

            public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
            {
                return Array.Empty<ValidationIssue>();
            }
        }

        public abstract class AbstractRule<TMarker> : IAssetRule
        {
            public abstract RuleDescriptor Descriptor { get; }

            public abstract bool CanEvaluate(AssetContext context);

            public abstract IEnumerable<ValidationIssue> Evaluate(AssetContext context);
        }

        public sealed class RuleWithoutParameterlessConstructor<TMarker> : IAssetRule
        {
            public RuleWithoutParameterlessConstructor(string value)
            {
            }

            public RuleDescriptor Descriptor => null;

            public bool CanEvaluate(AssetContext context)
            {
                return false;
            }

            public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
            {
                return Array.Empty<ValidationIssue>();
            }
        }

        public sealed class ThrowingConstructorRule<TMarker> : IAssetRule
        {
            public ThrowingConstructorRule()
            {
                throw new InvalidOperationException("Expected constructor failure.");
            }

            public RuleDescriptor Descriptor => null;

            public bool CanEvaluate(AssetContext context)
            {
                return false;
            }

            public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
            {
                return Array.Empty<ValidationIssue>();
            }
        }

        public sealed class NullDescriptorRule<TMarker> : IAssetRule
        {
            public RuleDescriptor Descriptor => null;

            public bool CanEvaluate(AssetContext context)
            {
                return false;
            }

            public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
            {
                return Array.Empty<ValidationIssue>();
            }
        }

        public sealed class DuplicateRule<TMarker> : IAssetRule
        {
            private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
                "test.duplicate",
                "Duplicate Test Rule",
                "Used to verify duplicate rule ID validation.",
                RuleSeverity.Info);

            public RuleDescriptor Descriptor => DescriptorValue;

            public bool CanEvaluate(AssetContext context)
            {
                return true;
            }

            public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
            {
                return Array.Empty<ValidationIssue>();
            }
        }
    }
}
