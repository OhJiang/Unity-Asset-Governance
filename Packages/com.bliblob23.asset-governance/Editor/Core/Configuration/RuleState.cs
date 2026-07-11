using System;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 保存单条规则的通用启用状态。
    /// </summary>
    [Serializable]
    public sealed class RuleState
    {
        [SerializeField]
        private string ruleId = string.Empty;

        [SerializeField]
        private bool enabled = true;

        public string RuleId => ruleId;

        public bool Enabled => enabled;
    }
}
