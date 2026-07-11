using System;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 保存单条规则的通用项目状态。
    /// </summary>
    [Serializable]
    public sealed class RuleState
    {
        [SerializeField]
        private string ruleId = string.Empty;

        [SerializeField]
        private bool enabled = true;

        [SerializeField]
        private bool overrideSeverity;

        [SerializeField]
        private RuleSeverity severity = RuleSeverity.Warning;

        public string RuleId => ruleId;

        public bool Enabled => enabled;

        public bool OverrideSeverity => overrideSeverity;

        public RuleSeverity Severity => severity;
    }
}
