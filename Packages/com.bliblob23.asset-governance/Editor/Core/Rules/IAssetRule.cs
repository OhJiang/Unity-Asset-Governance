using System.Collections.Generic;

namespace UnityAssetGovernance
{
    /// <summary>
    /// Public extension point for asset validation rules.
    /// Implementations should evaluate assets without modifying them.
    /// </summary>
    public interface IAssetRule
    {
        RuleDescriptor Descriptor { get; }

        bool CanEvaluate(AssetContext context);

        IEnumerable<ValidationIssue> Evaluate(AssetContext context);
    }
}
