using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance.Tests
{
    public sealed class BatchFixRunnerTests
    {
        [Test]
        public void Fix_RefreshesContextForEveryIssueAndContinuesAfterFailure()
        {
            const string assetPath = "Assets/Batch.png";
            var firstIssue = CreateIssue("test.success", assetPath, true);
            var failedIssue = CreateIssue("test.failure", assetPath, true);
            var lastIssue = CreateIssue("test.last", assetPath, true);
            var contexts = new List<AssetContext>();
            var executedRuleIds = new List<string>();

            IReadOnlyList<AssetContext> ScanAsset(string path)
            {
                var context = CreateContext(path, contexts.Count.ToString());
                contexts.Add(context);
                return new[] { context };
            }

            var result = BatchFixRunner.Fix(
                new[] { firstIssue, failedIssue, lastIssue },
                ScanAsset,
                new IAssetRule[]
                {
                    new TestFixableRule("test.success", executedRuleIds, false),
                    new TestFixableRule("test.failure", executedRuleIds, true),
                    new TestFixableRule("test.last", executedRuleIds, false)
                });

            Assert.That(contexts, Has.Count.EqualTo(3));
            Assert.That(contexts[0], Is.Not.SameAs(contexts[1]));
            Assert.That(contexts[1], Is.Not.SameAs(contexts[2]));
            Assert.That(
                executedRuleIds,
                Is.EqualTo(new[] { "test.success", "test.failure", "test.last" }));
            Assert.That(result.SucceededCount, Is.EqualTo(2));
            Assert.That(result.FailedCount, Is.EqualTo(1));
            Assert.That(result.SkippedCount, Is.Zero);
            Assert.That(result.FixResults[1].ExecutionError.Exception.Message, Does.Contain("Expected failure"));
        }

        [Test]
        public void Fix_SkipsIssuesThatAreNotFixableOrNoLongerScannable()
        {
            var readOnlyIssue = CreateIssue("test.read-only", "Assets/ReadOnly.asset", false);
            var excludedIssue = CreateIssue("test.excluded", "Assets/Excluded.asset", true);
            var scanCount = 0;

            var result = BatchFixRunner.Fix(
                new[] { readOnlyIssue, excludedIssue },
                _ =>
                {
                    scanCount++;
                    return Array.Empty<AssetContext>();
                },
                Array.Empty<IAssetRule>());

            Assert.That(scanCount, Is.EqualTo(1));
            Assert.That(result.SucceededCount, Is.Zero);
            Assert.That(result.FailedCount, Is.Zero);
            Assert.That(result.SkippedCount, Is.EqualTo(2));
            Assert.That(result.SkippedIssues, Is.EqualTo(new[] { readOnlyIssue, excludedIssue }));
        }

        [Test]
        public void Fix_ConvertsScanExceptionToFailureAndContinues()
        {
            var failedIssue = CreateIssue("test.failure", "Assets/Missing.asset", true);
            var successfulIssue = CreateIssue("test.success", "Assets/Available.asset", true);

            var result = BatchFixRunner.Fix(
                new[] { failedIssue, successfulIssue },
                path =>
                {
                    if (path == failedIssue.AssetPath)
                    {
                        throw new InvalidOperationException("Expected scan failure.");
                    }

                    return new[] { CreateContext(path, "available") };
                },
                new IAssetRule[]
                {
                    new TestFixableRule("test.success", new List<string>(), false)
                });

            Assert.That(result.SucceededCount, Is.EqualTo(1));
            Assert.That(result.FailedCount, Is.EqualTo(1));
            Assert.That(result.SkippedCount, Is.Zero);
            Assert.That(result.FixResults[0].ExecutionError.Stage, Is.EqualTo(RuleExecutionStage.Fix));
            Assert.That(result.FixResults[0].ExecutionError.Exception.Message, Does.Contain("Expected scan failure"));
        }

        private static AssetContext CreateContext(string assetPath, string guid)
        {
            return new AssetContext(
                guid,
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
            private readonly IList<string> _executedRuleIds;
            private readonly bool _throwWhenFixing;

            public TestFixableRule(
                string ruleId,
                IList<string> executedRuleIds,
                bool throwWhenFixing)
            {
                Descriptor = new RuleDescriptor(
                    ruleId,
                    "Test Rule",
                    "Test batch fix rule.",
                    RuleSeverity.Warning);
                _executedRuleIds = executedRuleIds;
                _throwWhenFixing = throwWhenFixing;
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

            public bool CanFix(AssetContext context, ValidationIssue issue)
            {
                return true;
            }

            public void Fix(AssetContext context, ValidationIssue issue)
            {
                _executedRuleIds.Add(Descriptor.Id);
                if (_throwWhenFixing)
                {
                    throw new InvalidOperationException("Expected failure.");
                }
            }
        }
    }
}
