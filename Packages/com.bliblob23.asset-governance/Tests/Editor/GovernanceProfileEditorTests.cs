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

        [Test]
        public void BuildRuleSelectionSummary_LimitsVisibleRuleIds()
        {
            var summary = GovernanceProfileEditor.BuildRuleSelectionSummary(new[]
            {
                "RULE-A",
                "RULE-B",
                "RULE-C",
                "RULE-D"
            });

            Assert.That(summary, Is.EqualTo("RULE-A, RULE-B, RULE-C and 1 more"));
        }

        [Test]
        public void RuleSelectionPopup_MatchesRuleIdAndDisplayNameIgnoringCase()
        {
            var option = new GovernanceProfileEditor.RuleOption(
                "UAG-TEX-001",
                "UAG-TEX-001 — UI Texture Mipmaps Must Be Disabled");

            Assert.That(RuleSelectionPopup.MatchesSearch(option, "tex-001"), Is.True);
            Assert.That(RuleSelectionPopup.MatchesSearch(option, "mipmaps"), Is.True);
            Assert.That(RuleSelectionPopup.MatchesSearch(option, "audio"), Is.False);
        }

        [Test]
        public void AssetPathField_GetProjectAssetPath_ReturnsProjectFolderPath()
        {
            const string parentPath = "Assets";
            var folderName = $"AssetPathFieldTests_{Guid.NewGuid():N}";
            var folderPath = $"{parentPath}/{folderName}";

            try
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
                var folder = AssetDatabase.LoadMainAssetAtPath(folderPath);

                Assert.That(AssetPathField.GetProjectAssetPath(folder), Is.EqualTo(folderPath));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
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
