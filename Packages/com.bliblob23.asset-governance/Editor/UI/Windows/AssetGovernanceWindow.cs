using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 为当前选择的资源提供最小可用的手动检查入口。
    /// </summary>
    public sealed class AssetGovernanceWindow : EditorWindow
    {
        private readonly HashSet<ValidationIssue> _selectedFixableIssues =
            new HashSet<ValidationIssue>();
        private Vector2 _scrollPosition;
        private string _issueSearchText = string.Empty;
        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showInfo = true;
        private bool _showOnlyFixable;
        private ValidationRunResult _lastResult;
        private BatchFixResult _lastBatchFixResult;
        private string _statusMessage = "Select assets or folders, then run validation.";

        internal ValidationRunResult LastResult => _lastResult;

        internal BatchFixResult LastBatchFixResult => _lastBatchFixResult;

        internal int SelectedFixableIssueCount => _selectedFixableIssues.Count;

        internal string StatusMessage => _statusMessage;

        [MenuItem("Tools/Asset Governance")]
        public static void Open()
        {
            GetWindow<AssetGovernanceWindow>("Asset Governance");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Unity Asset Governance", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);

            if (GUILayout.Button("Scan Selection"))
            {
                ScanSelection();
            }

            if (_lastResult == null)
            {
                return;
            }

            EditorGUILayout.Space();
            DrawIssueFilters();
            var visibleIssues = FilterIssues(
                _lastResult.Issues,
                _issueSearchText,
                _showErrors,
                _showWarnings,
                _showInfo,
                _showOnlyFixable);

            EditorGUILayout.LabelField(
                $"Issues: {_lastResult.Issues.Count}    Visible: {visibleIssues.Count}    " +
                $"Execution Errors: {_lastResult.ExecutionErrors.Count}",
                EditorStyles.boldLabel);

            DrawBatchFixActions(visibleIssues);
            if (_lastResult == null)
            {
                return;
            }

            visibleIssues = FilterIssues(
                _lastResult.Issues,
                _issueSearchText,
                _showErrors,
                _showWarnings,
                _showInfo,
                _showOnlyFixable);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawIssues(visibleIssues, _lastResult.Issues.Count);
            DrawExecutionErrors(_lastResult.ExecutionErrors);
            DrawBatchFixErrors(_lastBatchFixResult);
            EditorGUILayout.EndScrollView();
        }

        internal bool ScanSelection()
        {
            _selectedFixableIssues.Clear();
            _lastBatchFixResult = null;

            var assetPaths = CollectSelectedAssetPaths();
            if (assetPaths.Count == 0)
            {
                _lastResult = null;
                _statusMessage = "No assets or folders are selected.";
                Repaint();
                return false;
            }

            try
            {
                var contexts = AssetScanner.Scan(assetPaths);
                _lastResult = RuleRunner.Run(contexts);
                _statusMessage = $"Scanned {contexts.Count} asset(s).";
            }
            catch (Exception exception)
            {
                _lastResult = null;
                _statusMessage = $"Scan failed: {exception.Message}";
                Repaint();
                return false;
            }

            Repaint();
            return true;
        }

        internal static IReadOnlyList<string> CollectSelectedAssetPaths()
        {
            var uniquePaths = new HashSet<string>(StringComparer.Ordinal);

            foreach (var selectedObject in Selection.objects)
            {
                var assetPath = AssetDatabase.GetAssetPath(selectedObject);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    uniquePaths.Add(assetPath);
                }
            }

            var sortedPaths = new List<string>(uniquePaths);
            sortedPaths.Sort(StringComparer.Ordinal);
            return new ReadOnlyCollection<string>(sortedPaths);
        }

        internal static void LocateAsset(string assetPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        internal void SetIssueSelected(ValidationIssue issue, bool selected)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            if (!issue.CanFix || _lastResult == null || !_lastResult.Issues.Contains(issue))
            {
                _selectedFixableIssues.Remove(issue);
                return;
            }

            if (selected)
            {
                _selectedFixableIssues.Add(issue);
            }
            else
            {
                _selectedFixableIssues.Remove(issue);
            }
        }

        internal void SelectAllFixableIssues()
        {
            SelectAllFixableIssues(_lastResult?.Issues);
        }

        internal void SelectAllFixableIssues(IReadOnlyList<ValidationIssue> issues)
        {
            _selectedFixableIssues.Clear();
            if (issues == null)
            {
                return;
            }

            foreach (var issue in issues)
            {
                if (issue.CanFix)
                {
                    _selectedFixableIssues.Add(issue);
                }
            }
        }

        internal void ClearSelectedIssues()
        {
            _selectedFixableIssues.Clear();
        }

        private void DrawIssueFilters()
        {
            _issueSearchText = EditorGUILayout.TextField("Search", _issueSearchText);

            EditorGUILayout.BeginHorizontal();
            _showErrors = EditorGUILayout.ToggleLeft("Error", _showErrors, GUILayout.Width(70f));
            _showWarnings = EditorGUILayout.ToggleLeft("Warning", _showWarnings, GUILayout.Width(80f));
            _showInfo = EditorGUILayout.ToggleLeft("Info", _showInfo, GUILayout.Width(60f));
            _showOnlyFixable = EditorGUILayout.ToggleLeft(
                "Fixable Only",
                _showOnlyFixable,
                GUILayout.Width(100f));
            EditorGUILayout.EndHorizontal();
        }

        internal static IReadOnlyList<ValidationIssue> FilterIssues(
            IEnumerable<ValidationIssue> issues,
            string searchText,
            bool showErrors,
            bool showWarnings,
            bool showInfo,
            bool showOnlyFixable)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            var normalizedSearchText = string.IsNullOrWhiteSpace(searchText)
                ? string.Empty
                : searchText.Trim();
            var filteredIssues = new List<ValidationIssue>();

            foreach (var issue in issues)
            {
                if (issue == null)
                {
                    throw new ArgumentException(
                        "Issue collections cannot contain null.",
                        nameof(issues));
                }

                if (!IsSeverityVisible(
                        issue.Severity,
                        showErrors,
                        showWarnings,
                        showInfo) ||
                    (showOnlyFixable && !issue.CanFix) ||
                    !MatchesSearch(issue, normalizedSearchText))
                {
                    continue;
                }

                filteredIssues.Add(issue);
            }

            return new ReadOnlyCollection<ValidationIssue>(filteredIssues);
        }

        private static bool IsSeverityVisible(
            RuleSeverity severity,
            bool showErrors,
            bool showWarnings,
            bool showInfo)
        {
            switch (severity)
            {
                case RuleSeverity.Error:
                    return showErrors;
                case RuleSeverity.Warning:
                    return showWarnings;
                case RuleSeverity.Info:
                    return showInfo;
                default:
                    return false;
            }
        }

        private static bool MatchesSearch(
            ValidationIssue issue,
            string searchText)
        {
            return string.IsNullOrEmpty(searchText) ||
                   issue.RuleId.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   issue.AssetPath.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   issue.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawBatchFixActions(IReadOnlyList<ValidationIssue> issues)
        {
            var hasVisibleFixableIssue = issues.Any(issue => issue.CanFix);
            if (!hasVisibleFixableIssue && _selectedFixableIssues.Count == 0)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!hasVisibleFixableIssue);
            if (GUILayout.Button("Select All Visible Fixable"))
            {
                SelectAllFixableIssues(issues);
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Clear Selection"))
            {
                ClearSelectedIssues();
            }

            EditorGUI.BeginDisabledGroup(_selectedFixableIssues.Count == 0);
            if (GUILayout.Button($"Fix Selected ({_selectedFixableIssues.Count})"))
            {
                ConfirmAndFixSelectedIssues();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void ConfirmAndFixSelectedIssues()
        {
            var selectedIssues = GetSelectedIssues();
            var assetCount = selectedIssues
                .Select(issue => issue.AssetPath)
                .Distinct(StringComparer.Ordinal)
                .Count();

            var confirmed = EditorUtility.DisplayDialog(
                "Confirm Batch Fix",
                $"Fix {selectedIssues.Count} selected issue(s) across {assetCount} asset(s)?\n\n" +
                "Only the selected issues will be processed. Each asset will be refreshed before its fix.",
                "Fix",
                "Cancel");

            if (confirmed)
            {
                FixSelectedIssues();
            }
        }

        private void DrawIssues(
            IReadOnlyList<ValidationIssue> issues,
            int totalIssueCount)
        {
            if (issues.Count == 0)
            {
                var message = totalIssueCount == 0
                    ? "No asset issues found."
                    : "No asset issues match the current filters.";
                EditorGUILayout.HelpBox(message, MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Asset Issues", EditorStyles.boldLabel);
            foreach (var issue in issues)
            {
                EditorGUILayout.BeginHorizontal();
                if (issue.CanFix)
                {
                    var selected = EditorGUILayout.Toggle(
                        _selectedFixableIssues.Contains(issue),
                        GUILayout.Width(18f));
                    SetIssueSelected(issue, selected);
                }
                else
                {
                    GUILayout.Space(22f);
                }

                if (GUILayout.Button(
                    $"[{issue.Severity}] {issue.RuleId} | {issue.AssetPath}\n{issue.Message}",
                    EditorStyles.helpBox))
                {
                    LocateAsset(issue.AssetPath);
                }

                if (issue.CanFix && GUILayout.Button("Fix", GUILayout.Width(56f)))
                {
                    FixIssue(issue);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        internal bool FixIssue(ValidationIssue issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            try
            {
                var contexts = AssetScanner.Scan(new[] { issue.AssetPath });
                if (contexts.Count == 0)
                {
                    _statusMessage =
                        $"Fix failed: Asset '{issue.AssetPath}' is unavailable for validation.";
                    Repaint();
                    return false;
                }

                var fixResult = FixRunner.Fix(contexts[0], issue);
                if (!fixResult.Succeeded)
                {
                    _statusMessage = $"Fix failed: {fixResult.ExecutionError.Exception.Message}";
                    Repaint();
                    return false;
                }

                if (!ScanSelection())
                {
                    return false;
                }

                _statusMessage = $"Fixed {issue.RuleId} and rescanned selection.";
                Repaint();
                return true;
            }
            catch (Exception exception)
            {
                _statusMessage = $"Fix failed: {exception.Message}";
                Repaint();
                return false;
            }
        }

        internal bool FixSelectedIssues()
        {
            var selectedIssues = GetSelectedIssues();
            if (selectedIssues.Count == 0)
            {
                _statusMessage = "No fixable issues are selected.";
                Repaint();
                return false;
            }

            try
            {
                var batchResult = BatchFixRunner.Fix(selectedIssues);
                var rescanned = ScanSelection();
                var rescanStatus = _statusMessage;
                _lastBatchFixResult = batchResult;
                _statusMessage =
                    $"Batch fix completed: {batchResult.SucceededCount} succeeded, " +
                    $"{batchResult.FailedCount} failed, {batchResult.SkippedCount} skipped.";

                if (!rescanned)
                {
                    _statusMessage += $" Rescan failed: {rescanStatus}";
                }

                Repaint();
                return true;
            }
            catch (Exception exception)
            {
                _statusMessage = $"Batch fix failed: {exception.Message}";
                Repaint();
                return false;
            }
        }

        private IReadOnlyList<ValidationIssue> GetSelectedIssues()
        {
            if (_lastResult == null)
            {
                return Array.Empty<ValidationIssue>();
            }

            return _lastResult.Issues
                .Where(issue => _selectedFixableIssues.Contains(issue))
                .ToList()
                .AsReadOnly();
        }

        private static void DrawExecutionErrors(IReadOnlyList<RuleExecutionError> executionErrors)
        {
            if (executionErrors.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rule Execution Errors", EditorStyles.boldLabel);
            foreach (var executionError in executionErrors)
            {
                EditorGUILayout.HelpBox(
                    $"{executionError.RuleId} | {executionError.AssetPath} | {executionError.Stage}\n" +
                    executionError.Exception.Message,
                    MessageType.Error);
            }
        }

        private static void DrawBatchFixErrors(BatchFixResult batchFixResult)
        {
            if (batchFixResult == null || batchFixResult.FailedCount == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Batch Fix Errors", EditorStyles.boldLabel);
            foreach (var fixResult in batchFixResult.FixResults)
            {
                if (fixResult.Succeeded)
                {
                    continue;
                }

                var executionError = fixResult.ExecutionError;
                EditorGUILayout.HelpBox(
                    $"{executionError.RuleId} | {executionError.AssetPath} | {executionError.Stage}\n" +
                    executionError.Exception.Message,
                    MessageType.Error);
            }
        }
    }
}
