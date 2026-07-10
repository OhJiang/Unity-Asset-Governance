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

## Rule Execution

`RuleRunner.Run()` combines scanned `AssetContext` instances with automatically discovered or
explicitly supplied rules. It calls `CanEvaluate()` before `Evaluate()`, gathers issues, and returns
a deterministic `ValidationRunResult`. Exceptions from one rule are captured as
`RuleExecutionError` records and do not prevent other rules or assets from being checked.

```csharp
var contexts = AssetScanner.Scan(new[] { "Assets/Textures" });
var result = RuleRunner.Run(contexts);
```

Resource violations are available through `result.Issues`; framework execution failures are kept
separately in `result.ExecutionErrors`.

## Built-in Rules

### UAG-NAME-001: Asset Paths Must Not Contain Spaces

This warning applies to every asset type. It reports an issue when any part of the asset path,
including parent folders or the file name, contains a normal space character. The rule is read-only
because automatically renaming assets can affect external tools and workflows.

## Manual Validation Window

Open `Tools > Asset Governance`, select one or more assets or folders in the Project window, and
click **Scan Selection**. Selected folders are expanded recursively through `AssetScanner`. The
window displays asset issues separately from rule execution errors. Click an asset issue to select
and ping the corresponding asset in the Project window.

## Planned Documentation

- Installation
- Configuration
- Writing custom rules
- Writing custom rule settings
- Automatic fixes
- CI integration
