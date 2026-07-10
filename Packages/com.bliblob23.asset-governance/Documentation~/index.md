# Unity Asset Governance Documentation

Unity Asset Governance is an extensible, configuration-driven asset validation framework for the Unity Editor.

## Rule Extension Contract

Custom validation rules implement `IAssetRule`. Rules expose immutable metadata through
`RuleDescriptor`, decide whether they support an `AssetContext`, and return zero or more
`ValidationIssue` instances. Rule discovery and execution will be added in later milestones;
custom rules will not require registration in a central hard-coded list.

## Planned Documentation

- Installation
- Configuration
- Writing custom rules
- Writing custom rule settings
- Automatic fixes
- CI integration
