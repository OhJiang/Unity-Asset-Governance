using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 定位项目中唯一的默认 GovernanceProfile。
    /// </summary>
    public static class GovernanceProfileLocator
    {
        /// <summary>
        /// 返回项目中唯一的 GovernanceProfile；未创建时返回空引用，存在多个时抛出明确异常。
        /// </summary>
        public static GovernanceProfile LoadDefault()
        {
            var profilePaths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:GovernanceProfile"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    profilePaths.Add(path);
                }
            }

            return LoadDefault(profilePaths);
        }

        internal static GovernanceProfile LoadDefault(IReadOnlyList<string> profilePaths)
        {
            if (profilePaths == null)
            {
                throw new ArgumentNullException(nameof(profilePaths));
            }

            var sortedProfilePaths = new List<string>(profilePaths);
            sortedProfilePaths.Sort(StringComparer.Ordinal);

            if (sortedProfilePaths.Count == 0)
            {
                return null;
            }

            if (sortedProfilePaths.Count > 1)
            {
                throw new InvalidOperationException(
                    "Only one default GovernanceProfile is supported. Found: " +
                    string.Join(", ", sortedProfilePaths));
            }

            var profile = AssetDatabase.LoadAssetAtPath<GovernanceProfile>(sortedProfilePaths[0]);
            if (profile == null)
            {
                throw new InvalidOperationException(
                    $"Unable to load GovernanceProfile at '{sortedProfilePaths[0]}'.");
            }

            return profile;
        }
    }
}
