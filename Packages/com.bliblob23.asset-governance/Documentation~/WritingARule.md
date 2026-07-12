# Writing A Custom Rule

Custom rules can live in any Editor assembly that references `UnityAssetGovernance.Editor`. They do
not require changes to the framework source or a central registration list.

## 1. Create An Editor Assembly

Create an assembly definition with `Editor` in **Include Platforms** and add this reference:

```json
"references": [
  "UnityAssetGovernance.Editor"
]
```

## 2. Implement IAssetRule

A discoverable rule must be public, concrete, and have a public parameterless constructor.

```csharp
using System;
using System.Collections.Generic;
using UnityAssetGovernance;
using UnityEngine;

public sealed class TexturePrefixRule : IAssetRule
{
    private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
        "MYCOMPANY-TEX-001",
        "Textures Must Use A Prefix",
        "Configured textures must use the project naming prefix.",
        RuleSeverity.Warning,
        new[] { typeof(Texture2D) });

    public RuleDescriptor Descriptor => DescriptorValue;

    public bool CanEvaluate(AssetContext context)
    {
        return typeof(Texture2D).IsAssignableFrom(context.AssetType);
    }

    public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
    {
        if (context.AssetPath.StartsWith("Assets/Art/T_", StringComparison.Ordinal))
        {
            return Array.Empty<ValidationIssue>();
        }

        return new[]
        {
            new ValidationIssue(
                DescriptorValue.Id,
                DescriptorValue.DefaultSeverity,
                context.AssetPath,
                "Texture path does not use the required prefix.")
        };
    }
}
```

Rule IDs are serialized into profiles and reports. Choose a stable, globally unique prefix owned by
your organization and do not reuse or silently rename IDs.

`CanEvaluate()` and `Evaluate()` must not modify assets or importers. `RuleRunner` isolates exceptions
and records the failing execution stage instead of stopping the complete scan.

## 3. Add Strongly Typed Settings

Do not hard-code project paths, thresholds, or business exceptions. Create a rule-owned
`ScriptableObject` instead:

```csharp
using UnityAssetGovernance;
using UnityEngine;

[CreateAssetMenu(menuName = "Asset Governance/My Company/Texture Prefix Settings")]
public sealed class TexturePrefixRuleSettings : AssetRuleSettings
{
    [SerializeField]
    private string requiredPrefix = "T_";

    public override string RuleId => "MYCOMPANY-TEX-001";

    public string RequiredPrefix => requiredPrefix;
}
```

Read the settings through the scan context:

```csharp
if (context.GovernanceProfile == null ||
    !context.GovernanceProfile.TryGetRuleSettings(
        Descriptor.Id,
        out TexturePrefixRuleSettings settings))
{
    return false;
}
```

Add the created settings asset to **Governance Profile > Rule Settings**. The core profile stores the
public `AssetRuleSettings` base type, so it does not need to know the concrete custom settings class.

## 4. Add An Explicit Fix Only When Safe

Implement `IFixableAssetRule` only when the repair is deterministic and safe. Keep `Evaluate()`
read-only, re-check the current asset state in `CanFix()`, and modify the asset only in `Fix()`.

The framework confirms fix capability, isolates exceptions, asks for batch confirmation in the
Editor window, refreshes each asset context, and performs one final rescan. A rule must still save or
reimport its own importer changes inside `Fix()`.

Do not automate high-risk changes such as asset deletion, broad renaming, mesh scale changes, or
operations whose effects cannot be determined locally.

## 5. Test The Rule

At minimum, cover:

- A supported asset that passes.
- A supported asset that reports the expected issue.
- An unsupported asset that is skipped.
- Missing, invalid, and duplicate configuration when settings are used.
- `CanFix()`, `Fix()`, and validation after repair for fixable rules.
- Discovery through `RuleRegistry.DiscoverRules()` from the independent assembly.

Import **Custom Rule and Strongly Typed Settings** from the Package Manager Samples tab for a
complete independent assembly, settings, and EditMode test example.

## Compatibility Expectations

- Depend only on public framework types.
- Treat `AssetContext`, descriptors, and validation results as read-only inputs and outputs.
- Let the framework apply global exclusions, rule whitelists, enablement, and severity overrides.
- Avoid relying on discovery order; results are ordered by stable rule ID and asset path.
- Document any serialized settings changes and provide migration guidance before removing fields.
