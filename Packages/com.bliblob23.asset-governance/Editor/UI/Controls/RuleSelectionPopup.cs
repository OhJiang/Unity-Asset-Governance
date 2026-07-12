using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 在独立弹窗中搜索并多选规则，避免规则数量增加后撑高 Governance Profile Inspector。
    /// </summary>
    internal sealed class RuleSelectionPopup : PopupWindowContent
    {
        private readonly Object _target;
        private readonly string _propertyPath;
        private readonly IReadOnlyList<GovernanceProfileEditor.RuleOption> _options;
        private readonly Action _onChanged;
        private string _searchText = string.Empty;
        private Vector2 _scrollPosition;

        public RuleSelectionPopup(
            Object target,
            string propertyPath,
            IReadOnlyList<GovernanceProfileEditor.RuleOption> options,
            Action onChanged)
        {
            _target = target != null ? target : throw new ArgumentNullException(nameof(target));
            _propertyPath = !string.IsNullOrWhiteSpace(propertyPath)
                ? propertyPath
                : throw new ArgumentException("A property path is required.", nameof(propertyPath));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _onChanged = onChanged;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(520f, 420f);
        }

        public override void OnGUI(Rect rect)
        {
            var serializedTarget = new SerializedObject(_target);
            serializedTarget.UpdateIfRequiredOrScript();
            var ruleIds = serializedTarget.FindProperty(_propertyPath);
            if (ruleIds == null || !ruleIds.isArray)
            {
                EditorGUILayout.HelpBox("The whitelist rule list is no longer available.", MessageType.Error);
                return;
            }

            DrawHeader(ruleIds);
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawDiscoveredRules(ruleIds);
            DrawMissingRules(ruleIds);
            EditorGUILayout.EndScrollView();

            if (serializedTarget.ApplyModifiedProperties())
            {
                _onChanged?.Invoke();
            }
        }

        private static void DrawHeader(SerializedProperty ruleIds)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Ignored Rules ({ruleIds.arraySize} selected)",
                EditorStyles.boldLabel);
            if (GUILayout.Button("Clear All", GUILayout.Width(80f)))
            {
                ruleIds.ClearArray();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDiscoveredRules(SerializedProperty ruleIds)
        {
            var visibleRuleCount = 0;
            foreach (var option in _options)
            {
                if (!MatchesSearch(option, _searchText))
                {
                    continue;
                }

                visibleRuleCount++;
                var selected = GovernanceProfileEditor.ContainsRuleId(ruleIds, option.Id);
                var updated = EditorGUILayout.ToggleLeft(option.Label, selected);
                if (updated == selected)
                {
                    continue;
                }

                if (updated)
                {
                    GovernanceProfileEditor.AddRuleId(ruleIds, option.Id);
                }
                else
                {
                    GovernanceProfileEditor.RemoveRuleId(ruleIds, option.Id);
                }
            }

            if (visibleRuleCount == 0)
            {
                EditorGUILayout.HelpBox("No discovered rules match the search text.", MessageType.Info);
            }
        }

        private void DrawMissingRules(SerializedProperty ruleIds)
        {
            var missingRuleIds = GovernanceProfileEditor.GetMissingRuleIds(ruleIds, _options);
            if (missingRuleIds.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Missing Rules", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These saved rule IDs are not currently discovered. They are preserved until you remove them.",
                MessageType.Warning);

            foreach (var ruleId in missingRuleIds)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    string.IsNullOrWhiteSpace(ruleId) ? "(empty rule ID)" : ruleId);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    GovernanceProfileEditor.RemoveRuleId(ruleIds, ruleId);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        internal static bool MatchesSearch(
            GovernanceProfileEditor.RuleOption option,
            string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            return option.Id.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   option.Label.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
