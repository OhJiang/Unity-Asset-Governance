using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityAssetGovernance.Tests
{
    public sealed class GovernanceProfileEditorTests
    {
        [Test]
        public void CreateEditor_UsesGovernanceProfileCustomEditor()
        {
            var profile = ScriptableObject.CreateInstance<GovernanceProfile>();
            Editor editor = null;

            try
            {
                editor = Editor.CreateEditor(profile);

                Assert.That(editor, Is.TypeOf<GovernanceProfileEditor>());
            }
            finally
            {
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void CreateRuleOptions_IncludesThirdPartyRulesAndSortsByStableId()
        {
            var options = GovernanceProfileEditor.CreateRuleOptions(new IAssetRule[]
            {
                new EditorRuleB(),
                new EditorRuleA()
            });

            Assert.That(
                options.Select(option => option.Id),
                Is.EqualTo(new[] { "TEST-EDITOR-A", "TEST-EDITOR-B" }));
            Assert.That(
                options[0].Label,
                Is.EqualTo("TEST-EDITOR-A — Editor Rule A"));
        }

        [Test]
        public void FindDuplicateRuleIds_ReturnsEachNonEmptyDuplicateOnce()
        {
            var duplicates = GovernanceProfileEditor.FindDuplicateRuleIds(new[]
            {
                "UAG-TEX-001",
                string.Empty,
                "UAG-TEX-001",
                "UAG-NAME-001",
                "UAG-NAME-001",
                "UAG-NAME-001"
            });

            Assert.That(
                duplicates.OrderBy(ruleId => ruleId),
                Is.EqualTo(new[] { "UAG-NAME-001", "UAG-TEX-001" }));
        }

        [Test]
        public void FindDuplicateRuleIds_RejectsNullCollection()
        {
            Assert.Throws<ArgumentNullException>(() =>
                GovernanceProfileEditor.FindDuplicateRuleIds(null));
        }

        private sealed class EditorRuleA : IAssetRule
        {
            public RuleDescriptor Descriptor { get; } = new RuleDescriptor(
                "TEST-EDITOR-A",
                "Editor Rule A",
                "Editor rule A description.",
                RuleSeverity.Info);

            public bool CanEvaluate(AssetContext context)
            {
                return false;
            }

            public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
            {
                return Array.Empty<ValidationIssue>();
            }
        }

        private sealed class EditorRuleB : IAssetRule
        {
            public RuleDescriptor Descriptor { get; } = new RuleDescriptor(
                "TEST-EDITOR-B",
                "Editor Rule B",
                "Editor rule B description.",
                RuleSeverity.Info);

            public bool CanEvaluate(AssetContext context)
            {
                return false;
            }

            public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
            {
                return Array.Empty<ValidationIssue>();
            }
        }
    }
}
