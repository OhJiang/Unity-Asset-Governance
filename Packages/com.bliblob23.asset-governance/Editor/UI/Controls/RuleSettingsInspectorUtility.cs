using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 为 Governance Profile Inspector 提供规则配置类型发现、引用校验和资源创建能力。
    /// 只发现显式声明 CreateAssetMenu 的配置类型，避免把测试辅助类型意外暴露给使用者。
    /// </summary>
    internal static class RuleSettingsInspectorUtility
    {
        private const string SettingsFolderName = "Rule Settings";

        public static IReadOnlyList<RuleSettingsOption> DiscoverOptions(
            IReadOnlyList<GovernanceProfileEditor.RuleOption> ruleOptions)
        {
            var settingsTypes = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<AssetRuleSettings>())
            {
                if (!type.IsAbstract && !IsTestAssembly(type.Assembly))
                {
                    settingsTypes.Add(type);
                }
            }

            return CreateOptions(settingsTypes, ruleOptions);
        }

        internal static IReadOnlyList<RuleSettingsOption> CreateOptions(
            IEnumerable<Type> settingsTypes,
            IReadOnlyList<GovernanceProfileEditor.RuleOption> ruleOptions)
        {
            if (settingsTypes == null)
            {
                throw new ArgumentNullException(nameof(settingsTypes));
            }

            if (ruleOptions == null)
            {
                throw new ArgumentNullException(nameof(ruleOptions));
            }

            var options = new List<RuleSettingsOption>();
            var configuredRuleIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var settingsType in settingsTypes)
            {
                if (settingsType == null)
                {
                    throw new ArgumentException(
                        "Settings type collections cannot contain null.",
                        nameof(settingsTypes));
                }

                if (settingsType.IsAbstract ||
                    !typeof(AssetRuleSettings).IsAssignableFrom(settingsType))
                {
                    continue;
                }

                var createAssetMenu = settingsType.GetCustomAttribute<CreateAssetMenuAttribute>();
                if (createAssetMenu == null)
                {
                    continue;
                }

                var temporarySettings = ScriptableObject.CreateInstance(settingsType) as AssetRuleSettings;
                if (temporarySettings == null)
                {
                    throw new InvalidOperationException(
                        $"Rule settings type '{settingsType.FullName}' could not be created.");
                }

                string ruleId;
                try
                {
                    ruleId = temporarySettings.RuleId;
                }
                finally
                {
                    Object.DestroyImmediate(temporarySettings);
                }

                if (string.IsNullOrWhiteSpace(ruleId))
                {
                    throw new InvalidOperationException(
                        $"Rule settings type '{settingsType.FullName}' returned an empty rule ID.");
                }

                if (!configuredRuleIds.Add(ruleId))
                {
                    throw new InvalidOperationException(
                        $"More than one rule settings type declares rule ID '{ruleId}'.");
                }

                var fileName = string.IsNullOrWhiteSpace(createAssetMenu.fileName)
                    ? settingsType.Name
                    : Path.GetFileNameWithoutExtension(createAssetMenu.fileName);
                options.Add(new RuleSettingsOption(
                    ruleId,
                    settingsType,
                    FindRuleLabel(ruleOptions, ruleId, settingsType),
                    fileName));
            }

            options.Sort((left, right) =>
            {
                var ruleComparison = StringComparer.Ordinal.Compare(left.RuleId, right.RuleId);
                return ruleComparison != 0
                    ? ruleComparison
                    : StringComparer.Ordinal.Compare(
                        left.SettingsType.FullName,
                        right.SettingsType.FullName);
            });
            return new ReadOnlyCollection<RuleSettingsOption>(options);
        }

        public static bool TryAddReference(
            SerializedProperty ruleSettings,
            AssetRuleSettings settings,
            out string error)
        {
            if (ruleSettings == null)
            {
                throw new ArgumentNullException(nameof(ruleSettings));
            }

            if (settings == null)
            {
                error = "Select an existing Rule Settings asset.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(settings)))
            {
                error = "Rule Settings must be saved as a project asset before it can be added.";
                return false;
            }

            string ruleId;
            try
            {
                ruleId = settings.RuleId;
            }
            catch (Exception exception)
            {
                error = $"Could not read the selected settings Rule ID: {exception.Message}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ruleId))
            {
                error = $"Rule Settings '{settings.name}' has an empty Rule ID.";
                return false;
            }

            for (var index = 0; index < ruleSettings.arraySize; index++)
            {
                var existing = ruleSettings.GetArrayElementAtIndex(index).objectReferenceValue
                    as AssetRuleSettings;
                if (existing == settings)
                {
                    error = $"Rule Settings '{settings.name}' is already added.";
                    return false;
                }

                if (existing == null)
                {
                    continue;
                }

                string existingRuleId;
                try
                {
                    existingRuleId = existing.RuleId;
                }
                catch (Exception exception)
                {
                    error = $"Could not read Rule ID from '{existing.name}': {exception.Message}";
                    return false;
                }

                if (string.Equals(existingRuleId, ruleId, StringComparison.Ordinal))
                {
                    error = $"Rule '{ruleId}' already has Rule Settings in this profile.";
                    return false;
                }
            }

            var newIndex = ruleSettings.arraySize;
            ruleSettings.arraySize++;
            ruleSettings.GetArrayElementAtIndex(newIndex).objectReferenceValue = settings;
            error = null;
            return true;
        }

        public static AssetRuleSettings CreateAndAttach(
            GovernanceProfile profile,
            SerializedObject serializedProfile,
            SerializedProperty ruleSettings,
            RuleSettingsOption option)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (serializedProfile == null)
            {
                throw new ArgumentNullException(nameof(serializedProfile));
            }

            if (ruleSettings == null)
            {
                throw new ArgumentNullException(nameof(ruleSettings));
            }

            var profilePath = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrWhiteSpace(profilePath))
            {
                throw new InvalidOperationException(
                    "Save the Governance Profile as a project asset before creating Rule Settings.");
            }

            var profileFolder = Path.GetDirectoryName(profilePath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(profileFolder))
            {
                throw new InvalidOperationException(
                    $"Could not determine the folder for Governance Profile '{profile.name}'.");
            }

            var settingsFolder = EnsureSettingsFolder(profileFolder);
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{settingsFolder}/{option.FileName}.asset");
            var settings = ScriptableObject.CreateInstance(option.SettingsType) as AssetRuleSettings;
            if (settings == null)
            {
                throw new InvalidOperationException(
                    $"Rule settings type '{option.SettingsType.FullName}' could not be created.");
            }

            try
            {
                AssetDatabase.CreateAsset(settings, assetPath);
                Undo.RegisterCreatedObjectUndo(settings, "Create Rule Settings");

                serializedProfile.Update();
                if (!TryAddReference(ruleSettings, settings, out var error))
                {
                    throw new InvalidOperationException(error);
                }

                Undo.RecordObject(profile, "Attach Rule Settings");
                serializedProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                return settings;
            }
            catch
            {
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(settings)))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                else
                {
                    Object.DestroyImmediate(settings);
                }

                throw;
            }
        }

        private static string EnsureSettingsFolder(string profileFolder)
        {
            var settingsFolder = $"{profileFolder}/{SettingsFolderName}";
            if (AssetDatabase.IsValidFolder(settingsFolder))
            {
                return settingsFolder;
            }

            var folderGuid = AssetDatabase.CreateFolder(profileFolder, SettingsFolderName);
            if (string.IsNullOrWhiteSpace(folderGuid) || !AssetDatabase.IsValidFolder(settingsFolder))
            {
                throw new InvalidOperationException(
                    $"Could not create Rule Settings folder at '{settingsFolder}'.");
            }

            return settingsFolder;
        }

        private static string FindRuleLabel(
            IReadOnlyList<GovernanceProfileEditor.RuleOption> ruleOptions,
            string ruleId,
            Type settingsType)
        {
            foreach (var option in ruleOptions)
            {
                if (string.Equals(option.Id, ruleId, StringComparison.Ordinal))
                {
                    return option.Label;
                }
            }

            return $"{ruleId} — {settingsType.Name}";
        }

        private static bool IsTestAssembly(Assembly assembly)
        {
            return assembly.GetName().Name.EndsWith(".Tests", StringComparison.Ordinal);
        }

        internal readonly struct RuleSettingsOption
        {
            public RuleSettingsOption(
                string ruleId,
                Type settingsType,
                string label,
                string fileName)
            {
                RuleId = ruleId;
                SettingsType = settingsType;
                Label = label;
                FileName = fileName;
            }

            public string RuleId { get; }

            public Type SettingsType { get; }

            public string Label { get; }

            public string FileName { get; }
        }
    }
}
