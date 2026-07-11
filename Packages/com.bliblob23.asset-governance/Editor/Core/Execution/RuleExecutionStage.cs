namespace UnityAssetGovernance
{
    /// <summary>
    /// 标识规则执行失败时所处的阶段。
    /// </summary>
    public enum RuleExecutionStage
    {
        CanEvaluate = 0,
        Evaluate = 1,
        Configuration = 2
    }
}
