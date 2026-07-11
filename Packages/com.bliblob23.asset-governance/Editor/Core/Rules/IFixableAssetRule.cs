namespace UnityAssetGovernance
{
    /// <summary>
    /// 支持显式自动修复的资源规则扩展契约。
    /// 检查过程仍必须保持只读，只有 <see cref="Fix"/> 可以修改资源。
    /// </summary>
    public interface IFixableAssetRule : IAssetRule
    {
        /// <summary>
        /// 获取当前问题在当前资源状态下是否仍可安全修复。
        /// </summary>
        bool CanFix(AssetContext context, ValidationIssue issue);

        /// <summary>
        /// 执行单条问题的修复，并负责持久化必要的资源或 Importer 修改。
        /// </summary>
        void Fix(AssetContext context, ValidationIssue issue);
    }
}
