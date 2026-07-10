using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 为当前选择的资源提供最小可用的手动检查入口。
    /// </summary>
    public sealed class AssetGovernanceWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private ValidationRunResult _lastResult;
        private string _statusMessage = "Select assets or folders, then run validation.";

        internal ValidationRunResult LastResult => _lastResult;

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
            EditorGUILayout.LabelField(
                $"Issues: {_lastResult.Issues.Count}    Execution Errors: {_lastResult.ExecutionErrors.Count}",
                EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawIssues(_lastResult.Issues);
            DrawExecutionErrors(_lastResult.ExecutionErrors);
            EditorGUILayout.EndScrollView();
        }

        internal bool ScanSelection()
        {
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

        private static void DrawIssues(IReadOnlyList<ValidationIssue> issues)
        {
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No asset issues found.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Asset Issues", EditorStyles.boldLabel);
            foreach (var issue in issues)
            {
                if (GUILayout.Button(
                    $"[{issue.Severity}] {issue.RuleId} | {issue.AssetPath}\n{issue.Message}",
                    EditorStyles.helpBox))
                {
                    LocateAsset(issue.AssetPath);
                }
            }
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
    }
}
