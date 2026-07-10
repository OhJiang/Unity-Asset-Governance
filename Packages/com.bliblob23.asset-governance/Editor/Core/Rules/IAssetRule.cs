using System.Collections.Generic;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 资源验证规则的公共扩展点。
    /// 实现类应只检查资源，不应在检查过程中修改资源。
    /// </summary>
    public interface IAssetRule
    {
        RuleDescriptor Descriptor { get; }

        bool CanEvaluate(AssetContext context);

        IEnumerable<ValidationIssue> Evaluate(AssetContext context);
    }
}
