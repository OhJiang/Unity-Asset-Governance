namespace UnityAssetGovernance
{
    /// <summary>
    /// 描述验证问题对项目的影响程度。
    /// 枚举值按严重程度递增，便于调用方比较严重级别阈值。
    /// </summary>
    public enum RuleSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }
}
