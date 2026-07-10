using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class RuleRunnerTests
    {
        [Test]
        public void Run_CollectsIssuesOnlyFromApplicableRules()
        {
            var context = CreateContext("Assets/Textures/Hero.png");
            var applicableRule = CreateRule<ApplicableRuleMarker>(
                "test.applicable",
                _ => true,
                asset => new[] { CreateIssue("test.applicable", asset.AssetPath) });
            var skippedRule = CreateRule<SkippedRuleMarker>(
                "test.skipped",
                _ => false,
                _ => throw new InvalidOperationException("Skipped rules must not be evaluated."));

            var result = RuleRunner.Run(
                new[] { context },
                new IAssetRule[] { skippedRule, applicableRule });

            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].RuleId, Is.EqualTo("test.applicable"));
            Assert.That(result.ExecutionErrors, Is.Empty);
            Assert.That(result.HasExecutionErrors, Is.False);
        }

        [Test]
        public void Run_IsolatesCanEvaluateExceptionAndContinues()
        {
            var context = CreateContext("Assets/Textures/Hero.png");
            var throwingRule = CreateRule<CanEvaluateFailureMarker>(
                "test.can-evaluate-failure",
                _ => throw new InvalidOperationException("Expected CanEvaluate failure."),
                _ => Array.Empty<ValidationIssue>());
            var healthyRule = CreateRule<HealthyRuleMarker>(
                "test.healthy",
                _ => true,
                asset => new[] { CreateIssue("test.healthy", asset.AssetPath) });

            var result = RuleRunner.Run(
                new[] { context },
                new IAssetRule[] { throwingRule, healthyRule });

            Assert.That(result.Issues.Select(issue => issue.RuleId), Is.EqualTo(new[] { "test.healthy" }));
            Assert.That(result.ExecutionErrors, Has.Count.EqualTo(1));
            Assert.That(result.ExecutionErrors[0].RuleId, Is.EqualTo("test.can-evaluate-failure"));
            Assert.That(result.ExecutionErrors[0].AssetPath, Is.EqualTo(context.AssetPath));
            Assert.That(result.ExecutionErrors[0].Stage, Is.EqualTo(RuleExecutionStage.CanEvaluate));
            Assert.That(result.ExecutionErrors[0].Exception.Message, Does.Contain("Expected CanEvaluate failure"));
        }

        [Test]
        public void Run_PreservesProducedIssuesWhenEnumerationThrowsAndContinues()
        {
            var context = CreateContext("Assets/Textures/Hero.png");
            var throwingRule = CreateRule<EvaluateFailureMarker>(
                "test.evaluate-failure",
                _ => true,
                asset => YieldIssueThenThrow("test.evaluate-failure", asset.AssetPath));
            var healthyRule = CreateRule<HealthyRuleMarker>(
                "test.healthy",
                _ => true,
                asset => new[] { CreateIssue("test.healthy", asset.AssetPath) });

            var result = RuleRunner.Run(
                new[] { context },
                new IAssetRule[] { throwingRule, healthyRule });

            Assert.That(
                result.Issues.Select(issue => issue.RuleId),
                Is.EqualTo(new[] { "test.evaluate-failure", "test.healthy" }));
            Assert.That(result.ExecutionErrors, Has.Count.EqualTo(1));
            Assert.That(result.ExecutionErrors[0].RuleId, Is.EqualTo("test.evaluate-failure"));
            Assert.That(result.ExecutionErrors[0].Stage, Is.EqualTo(RuleExecutionStage.Evaluate));
            Assert.That(result.ExecutionErrors[0].Exception.Message, Does.Contain("Expected Evaluate failure"));
        }

        [Test]
        public void Run_ReportsNullIssueCollectionsAndEntriesAsExecutionErrors()
        {
            var context = CreateContext("Assets/Textures/Hero.png");
            var nullCollectionRule = CreateRule<NullCollectionMarker>(
                "test.null-collection",
                _ => true,
                _ => null);
            var nullEntryRule = CreateRule<NullEntryMarker>(
                "test.null-entry",
                _ => true,
                _ => new ValidationIssue[] { null });

            var result = RuleRunner.Run(
                new[] { context },
                new IAssetRule[] { nullEntryRule, nullCollectionRule });

            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.ExecutionErrors, Has.Count.EqualTo(2));
            Assert.That(
                result.ExecutionErrors.Select(error => error.RuleId),
                Is.EqualTo(new[] { "test.null-collection", "test.null-entry" }));
            Assert.That(result.ExecutionErrors.All(error => error.Stage == RuleExecutionStage.Evaluate), Is.True);
        }

        [Test]
        public void Run_SortsAssetsRulesAndIssuesDeterministically()
        {
            var firstContext = CreateContext("Assets/A.png");
            var secondContext = CreateContext("Assets/Z.png");
            var alphaRule = CreateRule<AlphaRuleMarker>(
                "test.alpha",
                _ => true,
                asset => new[] { CreateIssue("test.alpha", asset.AssetPath) });
            var zuluRule = CreateRule<ZuluRuleMarker>(
                "test.zulu",
                _ => true,
                asset => new[] { CreateIssue("test.zulu", asset.AssetPath) });

            var result = RuleRunner.Run(
                new[] { secondContext, firstContext },
                new IAssetRule[] { zuluRule, alphaRule });

            Assert.That(
                result.Issues.Select(issue => $"{issue.AssetPath}|{issue.RuleId}"),
                Is.EqualTo(new[]
                {
                    "Assets/A.png|test.alpha",
                    "Assets/A.png|test.zulu",
                    "Assets/Z.png|test.alpha",
                    "Assets/Z.png|test.zulu"
                }));
        }

        [Test]
        public void Run_ReturnsReadOnlyResultCollections()
        {
            var context = CreateContext("Assets/Textures/Hero.png");
            var rule = CreateRule<ApplicableRuleMarker>(
                "test.applicable",
                _ => true,
                asset => new[] { CreateIssue("test.applicable", asset.AssetPath) });

            var result = RuleRunner.Run(new[] { context }, new IAssetRule[] { rule });
            var mutableIssues = (IList<ValidationIssue>)result.Issues;
            var mutableErrors = (IList<RuleExecutionError>)result.ExecutionErrors;

            Assert.That(
                () => mutableIssues.Add(result.Issues[0]),
                Throws.TypeOf<NotSupportedException>());
            Assert.That(
                () => mutableErrors.Add(new RuleExecutionError(
                    rule.Descriptor.Id,
                    context.AssetPath,
                    RuleExecutionStage.Evaluate,
                    new InvalidOperationException())),
                Throws.TypeOf<NotSupportedException>());
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

        private static ValidationIssue CreateIssue(string ruleId, string assetPath)
        {
            return new ValidationIssue(
                ruleId,
                RuleSeverity.Warning,
                assetPath,
                "Test issue.");
        }

        private static DelegateRule<TMarker> CreateRule<TMarker>(
            string ruleId,
            Func<AssetContext, bool> canEvaluate,
            Func<AssetContext, IEnumerable<ValidationIssue>> evaluate)
        {
            return new DelegateRule<TMarker>(ruleId, canEvaluate, evaluate);
        }

        private static IEnumerable<ValidationIssue> YieldIssueThenThrow(
            string ruleId,
            string assetPath)
        {
            yield return CreateIssue(ruleId, assetPath);
            throw new InvalidOperationException("Expected Evaluate failure.");
        }

        private sealed class DelegateRule<TMarker> : IAssetRule
        {
            private readonly Func<AssetContext, bool> _canEvaluate;
            private readonly Func<AssetContext, IEnumerable<ValidationIssue>> _evaluate;

            public DelegateRule(
                string ruleId,
                Func<AssetContext, bool> canEvaluate,
                Func<AssetContext, IEnumerable<ValidationIssue>> evaluate)
            {
                Descriptor = new RuleDescriptor(
                    ruleId,
                    "Test Rule",
                    "Used to verify rule execution.",
                    RuleSeverity.Warning);
                _canEvaluate = canEvaluate;
                _evaluate = evaluate;
            }

            public RuleDescriptor Descriptor { get; }

            public bool CanEvaluate(AssetContext context)
            {
                return _canEvaluate(context);
            }

            public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
            {
                return _evaluate(context);
            }
        }

        private sealed class ApplicableRuleMarker
        {
        }

        private sealed class SkippedRuleMarker
        {
        }

        private sealed class CanEvaluateFailureMarker
        {
        }

        private sealed class EvaluateFailureMarker
        {
        }

        private sealed class HealthyRuleMarker
        {
        }

        private sealed class NullCollectionMarker
        {
        }

        private sealed class NullEntryMarker
        {
        }

        private sealed class AlphaRuleMarker
        {
        }

        private sealed class ZuluRuleMarker
        {
        }
    }
}
