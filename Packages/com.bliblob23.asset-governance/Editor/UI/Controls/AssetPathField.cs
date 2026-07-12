using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 绘制可手动编辑、可拖拽资源或文件夹的项目路径字段。
    /// 配置中仍然只保存字符串路径，避免让运行时数据依赖具体资源类型。
    /// </summary>
    internal static class AssetPathField
    {
        private const float ObjectFieldWidth = 90f;
        private const float PingButtonWidth = 48f;

        public static void Draw(GUIContent label, SerializedProperty pathProperty)
        {
            if (pathProperty == null)
            {
                throw new ArgumentNullException(nameof(pathProperty));
            }

            if (pathProperty.propertyType != SerializedPropertyType.String)
            {
                throw new ArgumentException("Asset path properties must be strings.", nameof(pathProperty));
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            pathProperty.stringValue = EditorGUILayout.TextField(pathProperty.stringValue);

            var currentAsset = LoadAsset(pathProperty.stringValue);
            var selectedAsset = EditorGUILayout.ObjectField(
                currentAsset,
                typeof(Object),
                false,
                GUILayout.Width(ObjectFieldWidth));
            if (selectedAsset != currentAsset)
            {
                pathProperty.stringValue = selectedAsset == null
                    ? string.Empty
                    : GetProjectAssetPath(selectedAsset);
            }

            EditorGUI.BeginDisabledGroup(currentAsset == null);
            if (GUILayout.Button("Ping", GUILayout.Width(PingButtonWidth)))
            {
                EditorGUIUtility.PingObject(currentAsset);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        internal static string GetProjectAssetPath(Object asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "Only assets and folders from the Project window can be used.",
                    nameof(asset));
            }

            return path.Replace('\\', '/').TrimEnd('/');
        }

        private static Object LoadAsset(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? null
                : AssetDatabase.LoadMainAssetAtPath(path.Trim());
        }
    }
}
