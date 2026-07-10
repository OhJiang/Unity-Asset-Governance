# Unity Asset Governance Documentation

Unity Asset Governance is an extensible, configuration-driven asset validation framework for the Unity Editor.

## Rule Extension Contract

Custom validation rules implement `IAssetRule`. Rules expose immutable metadata through
`RuleDescriptor`, decide whether they support an `AssetContext`, and return zero or more
`ValidationIssue` instances.

## Rule Discovery

`RuleRegistry.DiscoverRules()` uses Unity `TypeCache` to find concrete `IAssetRule`
implementations automatically. Rules must provide a public parameterless constructor and a
non-null descriptor with a unique ID. Discovery results are sorted by rule ID so execution is
deterministic across the Editor and BatchMode. Custom rules do not require registration in a
central hard-coded list.

## Asset Scanning

`AssetScanner.Scan()` accepts asset and folder paths. Folders are expanded recursively, duplicate
assets are removed, and results are sorted by asset path. Each result is represented by an
`AssetContext` containing the GUID, path, main asset type, importer, and active build target. The
scanner does not load the asset object by default, so rules that only need paths or importer data do
not pay an unnecessary loading cost.

## Planned Documentation

- Installation
- Configuration
- Writing custom rules
- Writing custom rule settings
- Automatic fixes
- CI integration
