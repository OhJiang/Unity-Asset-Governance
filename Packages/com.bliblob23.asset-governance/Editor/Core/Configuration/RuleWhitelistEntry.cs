using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 保存一个资源或文件夹针对指定规则的项目级白名单配置。
    /// </summary>
    [Serializable]
    public sealed class RuleWhitelistEntry
    {
        [SerializeField]
        private string assetPath;

        [SerializeField]
        private List<string> ruleIds = new List<string>();

        public string AssetPath => assetPath;

        public IReadOnlyList<string> RuleIds =>
            new ReadOnlyCollection<string>(ruleIds);
    }
}
