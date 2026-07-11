using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 规则专属强类型配置的公共基类。
    /// GovernanceProfile 只依赖此基类，因此第三方规则可以新增配置类型而无需修改框架核心。
    /// </summary>
    public abstract class AssetRuleSettings : ScriptableObject
    {
        /// <summary>
        /// 获取此配置所属规则的稳定 ID。
        /// </summary>
        public abstract string RuleId { get; }
    }
}
