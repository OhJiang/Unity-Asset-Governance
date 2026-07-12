using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 为 GovernanceProfile 提供规则感知的编辑体验，避免使用者手动输入规则 ID。
    /// </summary>
    [CustomEditor(typeof(GovernanceProfile))]
    internal sealed class GovernanceProfileEditor : Editor
    {
        private const string NoRuleSelectedLabel = "Select a rule";

        private SerializedProperty _excludedPaths;
        private SerializedProperty _whitelistEntries;
        private SerializedProperty _ruleStates;
        private SerializedProperty _ruleSettings;
        private IReadOnlyList<RuleOption> _ruleOptions = Array.Empty<RuleOption>();
        private string _ruleDiscoveryError;
        private bool _showRuleStates = true;
        private bool _showWhitelistEntries = true;

        private void OnEnable()
        {
            _excludedPaths = serializedObject.FindProperty("excludedPaths");
            _whitelistEntries = serializedObject.FindProperty("whitelistEntries");
            _ruleStates = serializedObject.FindProperty("ruleStates");
            _ruleSettings = serializedObject.FindProperty("ruleSettings");
            RefreshRuleOptions();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Governance Profile stores project-wide exclusions, per-rule whitelist entries, " +
                "rule enablement and severity overrides, and strongly typed rule settings. " +
                "The current release supports one profile in the project.",
                MessageType.Info);

            DrawRuleDiscoveryStatus();
            EditorGUILayout.PropertyField(_excludedPaths, true);
            DrawRuleStates();
            DrawWhitelistEntries();
            EditorGUILayout.PropertyField(_ruleSettings, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuleDiscoveryStatus()
        {
            if (!string.IsNullOrEmpty(_ruleDiscoveryError))
            {
                EditorGUILayout.HelpBox(
                    $"Rule discovery failed. Existing rule IDs are preserved, but rule selection " +
                    $"is unavailable.\n{_ruleDiscoveryError}",
                    MessageType.Error);
            }

            if (GUILayout.Button("Refresh Rule List"))
            {
                RefreshRuleOptions();
            }
        }

        private void DrawRuleStates()
        {
            EditorGUILayout.Space();
            _showRuleStates = EditorGUILayout.Foldout(
                _showRuleStates,
                $"Rule States ({_ruleStates.arraySize})",
                true);
            if (!_showRuleStates)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Add entries only when a rule must be disabled or its severity must be overridden. " +
                "Rules without an entry remain enabled with their default severity.",
                MessageType.None);

            var duplicateIds = FindDuplicateRuleIds(ReadRuleStateIds(_ruleStates));
            for (var index = 0; index < _ruleStates.arraySize; index++)
            {
                var state = _ruleStates.GetArrayElementAtIndex(index);
                var ruleId = state.FindPropertyRelative("ruleId");
                var enabled = state.FindPropertyRelative("enabled");
                var overrideSeverity = state.FindPropertyRelative("overrideSeverity");
                var severity = state.FindPropertyRelative("severity");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Rule State {index + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    _ruleStates.DeleteArrayElementAtIndex(index);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                DrawRulePopup("Rule", ruleId);
                EditorGUILayout.PropertyField(enabled);
                EditorGUILayout.PropertyField(overrideSeverity);
                if (overrideSeverity.boolValue)
                {
                    EditorGUILayout.PropertyField(severity);
                }

                DrawRuleIdWarnings(ruleId.stringValue, duplicateIds);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Rule State"))
            {
                AddRuleState();
            }
        }

        private void DrawWhitelistEntries()
        {
            EditorGUILayout.Space();
            _showWhitelistEntries = EditorGUILayout.Foldout(
                _showWhitelistEntries,
                $"Whitelist Entries ({_whitelistEntries.arraySize})",
                true);
            if (!_showWhitelistEntries)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "A whitelist entry skips only the selected rules for one asset or folder path. " +
                "Other rules continue to evaluate the same path.",
                MessageType.None);

            for (var index = 0; index < _whitelistEntries.arraySize; index++)
            {
                var entry = _whitelistEntries.GetArrayElementAtIndex(index);
                var assetPath = entry.FindPropertyRelative("assetPath");
                var ruleIds = entry.FindPropertyRelative("ruleIds");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Whitelist Entry {index + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    _whitelistEntries.DeleteArrayElementAtIndex(index);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(assetPath, new GUIContent("Asset Path"));
                if (string.IsNullOrWhiteSpace(assetPath.stringValue))
                {
                    EditorGUILayout.HelpBox("Asset Path is required.", MessageType.Error);
                }

                DrawWhitelistRuleIds(ruleIds);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Whitelist Entry"))
            {
                AddWhitelistEntry();
            }
        }

        private void DrawWhitelistRuleIds(SerializedProperty ruleIds)
        {
            EditorGUILayout.LabelField("Ignored Rules", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(_ruleDiscoveryError))
            {
                EditorGUILayout.PropertyField(ruleIds, true);
                return;
            }

            foreach (var option in _ruleOptions)
            {
                var selected = ContainsRuleId(ruleIds, option.Id);
                var updated = EditorGUILayout.ToggleLeft(option.Label, selected);
                if (updated == selected)
                {
                    continue;
                }

                if (updated)
                {
                    AddRuleId(ruleIds, option.Id);
                }
                else
                {
                    RemoveRuleId(ruleIds, option.Id);
                }
            }

            var missingRuleIds = GetMissingRuleIds(ruleIds, _ruleOptions);
            foreach (var missingRuleId in missingRuleIds)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(
                    string.IsNullOrWhiteSpace(missingRuleId)
                        ? "The whitelist contains an empty rule ID."
                        : $"Rule '{missingRuleId}' is not currently discovered.",
                    MessageType.Warning);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    RemoveRuleId(ruleIds, missingRuleId);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (ruleIds.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Select at least one rule for this whitelist entry.",
                    MessageType.Error);
            }

            var duplicateIds = FindDuplicateRuleIds(ReadStringArray(ruleIds));
            foreach (var duplicateId in duplicateIds)
            {
                EditorGUILayout.HelpBox(
                    $"Rule '{duplicateId}' is selected more than once.",
                    MessageType.Error);
            }
        }

        private void DrawRulePopup(string label, SerializedProperty ruleId)
        {
            if (!string.IsNullOrEmpty(_ruleDiscoveryError))
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(label, ruleId.stringValue);
                EditorGUI.EndDisabledGroup();
                return;
            }

            var labels = new List<string> { NoRuleSelectedLabel };
            var selectedIndex = 0;

            for (var index = 0; index < _ruleOptions.Count; index++)
            {
                labels.Add(_ruleOptions[index].Label);
                if (string.Equals(
                        _ruleOptions[index].Id,
                        ruleId.stringValue,
                        StringComparison.Ordinal))
                {
                    selectedIndex = index + 1;
                }
            }

            if (selectedIndex == 0 && !string.IsNullOrWhiteSpace(ruleId.stringValue))
            {
                labels.Add($"Missing: {ruleId.stringValue}");
                selectedIndex = labels.Count - 1;
            }

            var updatedIndex = EditorGUILayout.Popup(label, selectedIndex, labels.ToArray());
            if (updatedIndex == selectedIndex)
            {
                return;
            }

            ruleId.stringValue = updatedIndex == 0
                ? string.Empty
                : _ruleOptions[updatedIndex - 1].Id;
        }

        private void DrawRuleIdWarnings(string ruleId, ISet<string> duplicateIds)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                EditorGUILayout.HelpBox("Select a rule.", MessageType.Error);
                return;
            }

            if (duplicateIds.Contains(ruleId))
            {
                EditorGUILayout.HelpBox(
                    $"Rule '{ruleId}' has more than one Rule State.",
                    MessageType.Error);
            }

            if (string.IsNullOrEmpty(_ruleDiscoveryError) && !ContainsRuleOption(_ruleOptions, ruleId))
            {
                EditorGUILayout.HelpBox(
                    $"Rule '{ruleId}' is not currently discovered. The saved ID is preserved.",
                    MessageType.Warning);
            }
        }

        private void AddRuleState()
        {
            var index = _ruleStates.arraySize;
            _ruleStates.arraySize++;
            var state = _ruleStates.GetArrayElementAtIndex(index);
            state.FindPropertyRelative("ruleId").stringValue = string.Empty;
            state.FindPropertyRelative("enabled").boolValue = true;
            state.FindPropertyRelative("overrideSeverity").boolValue = false;
            state.FindPropertyRelative("severity").enumValueIndex = (int)RuleSeverity.Warning;
        }

        private void AddWhitelistEntry()
        {
            var index = _whitelistEntries.arraySize;
            _whitelistEntries.arraySize++;
            var entry = _whitelistEntries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("assetPath").stringValue = string.Empty;
            entry.FindPropertyRelative("ruleIds").ClearArray();
        }

        private void RefreshRuleOptions()
        {
            try
            {
                _ruleOptions = CreateRuleOptions(RuleRegistry.DiscoverRules());
                _ruleDiscoveryError = null;
            }
            catch (Exception exception)
            {
                _ruleOptions = Array.Empty<RuleOption>();
                _ruleDiscoveryError = exception.Message;
            }
        }

        internal static IReadOnlyList<RuleOption> CreateRuleOptions(
            IEnumerable<IAssetRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var options = new List<RuleOption>();
            foreach (var rule in rules)
            {
                if (rule == null)
                {
                    throw new ArgumentException(
                        "Rule collections cannot contain null.",
                        nameof(rules));
                }

                var descriptor = rule.Descriptor;
                if (descriptor == null)
                {
                    throw new InvalidOperationException(
                        $"Rule '{rule.GetType().FullName}' returned a null descriptor.");
                }

                options.Add(new RuleOption(
                    descriptor.Id,
                    $"{descriptor.Id} — {descriptor.DisplayName}"));
            }

            options.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.Id, right.Id));
            return new ReadOnlyCollection<RuleOption>(options);
        }

        internal static ISet<string> FindDuplicateRuleIds(IEnumerable<string> ruleIds)
        {
            if (ruleIds == null)
            {
                throw new ArgumentNullException(nameof(ruleIds));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var duplicates = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ruleId in ruleIds)
            {
                if (string.IsNullOrWhiteSpace(ruleId))
                {
                    continue;
                }

                if (!seen.Add(ruleId))
                {
                    duplicates.Add(ruleId);
                }
            }

            return duplicates;
        }

        private static IReadOnlyList<string> ReadRuleStateIds(SerializedProperty ruleStates)
        {
            var ruleIds = new List<string>(ruleStates.arraySize);
            for (var index = 0; index < ruleStates.arraySize; index++)
            {
                ruleIds.Add(
                    ruleStates.GetArrayElementAtIndex(index)
                        .FindPropertyRelative("ruleId")
                        .stringValue);
            }

            return ruleIds;
        }

        private static IReadOnlyList<string> ReadStringArray(SerializedProperty values)
        {
            var result = new List<string>(values.arraySize);
            for (var index = 0; index < values.arraySize; index++)
            {
                result.Add(values.GetArrayElementAtIndex(index).stringValue);
            }

            return result;
        }

        private static IReadOnlyList<string> GetMissingRuleIds(
            SerializedProperty ruleIds,
            IReadOnlyList<RuleOption> options)
        {
            var missingIds = new List<string>();
            for (var index = 0; index < ruleIds.arraySize; index++)
            {
                var ruleId = ruleIds.GetArrayElementAtIndex(index).stringValue;
                if (!ContainsRuleOption(options, ruleId) && !missingIds.Contains(ruleId))
                {
                    missingIds.Add(ruleId);
                }
            }

            return missingIds;
        }

        private static bool ContainsRuleOption(
            IReadOnlyList<RuleOption> options,
            string ruleId)
        {
            foreach (var option in options)
            {
                if (string.Equals(option.Id, ruleId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsRuleId(SerializedProperty ruleIds, string ruleId)
        {
            for (var index = 0; index < ruleIds.arraySize; index++)
            {
                if (string.Equals(
                        ruleIds.GetArrayElementAtIndex(index).stringValue,
                        ruleId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddRuleId(SerializedProperty ruleIds, string ruleId)
        {
            if (ContainsRuleId(ruleIds, ruleId))
            {
                return;
            }

            var index = ruleIds.arraySize;
            ruleIds.arraySize++;
            ruleIds.GetArrayElementAtIndex(index).stringValue = ruleId;
        }

        private static void RemoveRuleId(SerializedProperty ruleIds, string ruleId)
        {
            for (var index = ruleIds.arraySize - 1; index >= 0; index--)
            {
                if (string.Equals(
                        ruleIds.GetArrayElementAtIndex(index).stringValue,
                        ruleId,
                        StringComparison.Ordinal))
                {
                    ruleIds.DeleteArrayElementAtIndex(index);
                }
            }
        }

        internal readonly struct RuleOption
        {
            public RuleOption(string id, string label)
            {
                Id = id;
                Label = label;
            }

            public string Id { get; }

            public string Label { get; }
        }
    }
}
