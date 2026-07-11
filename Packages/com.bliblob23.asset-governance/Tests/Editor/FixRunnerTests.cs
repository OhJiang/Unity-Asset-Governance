using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class FixRunnerTests
    {
        [Test]
        public void RuleRunner_MarksIssueAsFixableWhenRuleConfirmsSupport()
        {
            var context = CreateContext("Assets/Fixable.png");
            var rule = new TestFixableRule(
                "test.fixable",
                (_, __) => true,
                (_, __) => { });

            var result = RuleRunner.Run(
                new[] { context },
                new IAssetRule[] { rule });

            Assert.That(result.Issues.Single().CanFix, Is.True);
            Assert.That(result.ExecutionErrors, Is.Empty);
        }

        [Test]
        public void RuleRunner_PreservesIssueAndReportsCanFixException()
        {
            var context = CreateContext("Assets/CanFixFailure.png");
            var rule = new TestFixableRule(
                "test.can-fix-failure",
                (_, __) => throw new InvalidOperationException("Expected CanFix failure."),
                (_, __) => { });

            var result = RuleRunner.Run(
                new[] { context },
                new IAssetRule[] { rule });

            Assert.That(result.Issues.Single().CanFix, Is.False);
            Assert.That(result.ExecutionErrors, Has.Count.EqualTo(1));
            Assert.That(result.ExecutionErrors[0].Stage, Is.EqualTo(RuleExecutionStage.CanFix));
            Assert.That(result.ExecutionErrors[0].Exception.Message, Does.Contain("Expected CanFix failure"));
        }

        [Test]
        public void Fix_ExecutesMatchingFixableRule()
        {
            var context = CreateContext("Assets/Fixable.png");
            var fixedIssue = false;
            var rule = new TestFixableRule(
                "test.fixable",
                (_, __) => true,
                (_, __) => fixedIssue = true);
            var issue = CreateIssue(rule.Descriptor.Id, context.AssetPath, true);

            var result = FixRunner.Fix(
                context,
                issue,
                new IAssetRule[] { rule });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ExecutionError, Is.Null);
            Assert.That(result.Issue, Is.SameAs(issue));
            Assert.That(fixedIssue, Is.True);
        }

        [Test]
        public void Fix_ReportsFixException()
        {
            var context = CreateContext("Assets/FixFailure.png");
            var rule = new TestFixableRule(
                "test.fix-failure",
                (_, __) => true,
                (_, __) => throw new InvalidOperationException("Expected Fix failure."));
            var issue = CreateIssue(rule.Descriptor.Id, context.AssetPath, true);

            var result = FixRunner.Fix(
                context,
                issue,
                new IAssetRule[] { rule });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ExecutionError.Stage, Is.EqualTo(RuleExecutionStage.Fix));
            Assert.That(result.ExecutionError.Exception.Message, Does.Contain("Expected Fix failure"));
        }

        [Test]
        public void Fix_ReportsWhenIssueIsNoLongerFixable()
        {
            var context = CreateContext("Assets/Stale.png");
            var rule = new TestFixableRule(
                "test.stale",
                (_, __) => false,
                (_, __) => throw new InvalidOperationException("Unavailable fixes must not execute."));
            var issue = CreateIssue(rule.Descriptor.Id, context.AssetPath, true);

            var result = FixRunner.Fix(
                context,
                issue,
                new IAssetRule[] { rule });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ExecutionError.Stage, Is.EqualTo(RuleExecutionStage.CanFix));
            Assert.That(result.ExecutionError.Exception.Message, Does.Contain("current state"));
        }

        [Test]
        public void Fix_RejectsIssueAndContextForDifferentAssets()
        {
            var context = CreateContext("Assets/Current.png");
            var rule = new TestFixableRule(
                "test.path-mismatch",
                (_, __) => true,
                (_, __) => { });
            var issue = CreateIssue(rule.Descriptor.Id, "Assets/Other.png", true);

            var result = FixRunner.Fix(
                context,
                issue,
                new IAssetRule[] { rule });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ExecutionError.Stage, Is.EqualTo(RuleExecutionStage.Fix));
            Assert.That(result.ExecutionError.Exception.Message, Does.Contain("does not match"));
        }

        [Test]
        public void Fix_RejectsRuleWithoutFixContract()
        {
            var context = CreateContext("Assets/ReadOnly.png");
            var rule = new ReadOnlyRule("test.read-only");
            var issue = CreateIssue(rule.Descriptor.Id, context.AssetPath, false);

            var result = FixRunner.Fix(
                context,
                issue,
                new IAssetRule[] { rule });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ExecutionError.Stage, Is.EqualTo(RuleExecutionStage.Fix));
            Assert.That(result.ExecutionError.Exception.Message, Does.Contain("does not provide"));
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

        private static ValidationIssue CreateIssue(
            string ruleId,
            string assetPath,
            bool canFix)
        {
            return new ValidationIssue(
                ruleId,
                RuleSeverity.Warning,
                assetPath,
                "Test issue.",
                canFix);
        }

        private sealed class TestFixableRule : IFixableAssetRule
        {
            private readonly Func<AssetContext, ValidationIssue, bool> _canFix;
            private readonly Action<AssetContext, ValidationIssue> _fix;

            public TestFixableRule(
                string ruleId,
                Func<AssetContext, ValidationIssue, bool> canFix,
                Action<AssetContext, ValidationIssue> fix)
            {
                Descriptor = new RuleDescriptor(
                    ruleId,
                    "Test Fixable Rule",
                    "Used to verify automatic fix execution.",
                    RuleSeverity.Warning);
                _canFix = canFix;
                _fix = fix;
            }

            public RuleDescriptor Descriptor { get; }

            public bool CanEvaluate(AssetContext context)
            {
                return true;
            }

            public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
            {
                return new[]
                {
                    new ValidationIssue(
                        Descriptor.Id,
                        Descriptor.DefaultSeverity,
                        context.AssetPath,
                        "Test issue.")
                };
            }

            public bool CanFix(AssetContext context, ValidationIssue issue)
            {
                return _canFix(context, issue);
            }

            public void Fix(AssetContext context, ValidationIssue issue)
            {
                _fix(context, issue);
            }
        }

        private sealed class ReadOnlyRule : IAssetRule
        {
            public ReadOnlyRule(string ruleId)
            {
                Descriptor = new RuleDescriptor(
                    ruleId,
                    "Read-only Rule",
                    "Used to verify fix rejection.",
                    RuleSeverity.Warning);
            }

            public RuleDescriptor Descriptor { get; }

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
