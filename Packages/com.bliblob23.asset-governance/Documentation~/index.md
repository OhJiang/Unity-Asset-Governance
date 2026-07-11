# Unity Asset Governance Documentation

Unity Asset Governance is an extensible, configuration-driven asset validation framework for the Unity Editor.

## Rule Extension Contract

Custom validation rules implement `IAssetRule`. Rules expose immutable metadata through
`RuleDescriptor`, decide whether they support an `AssetContext`, and return zero or more
`ValidationIssue` instances.

## Rule Discovery

`RuleRegistry.DiscoverRules()` uses Unity `TypeCache` to find concrete `IAssetRule`
implementations automatically. Rule classes must be public and provide a public parameterless
constructor plus a non-null descriptor with a unique ID. Non-public helper types are ignored.
Discovery results are sorted by rule ID so execution is
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

## Automatic Fix Extension Contract

A rule that can safely repair one of its own issues implements `IFixableAssetRule` in addition to
`IAssetRule`. `Evaluate()` must remain read-only. `CanFix()` checks the current asset state, while
`Fix()` is the only rule method allowed to modify an importer or asset and must persist the change.

`RuleRunner` calls `CanFix()` when collecting an issue and exposes the confirmed result through
`ValidationIssue.CanFix`. Rules cannot mark their own issues as fixable through the public issue
constructor. If `CanFix()` throws, the issue is preserved as read-only and the exception becomes a
`RuleExecutionStage.CanFix` execution error.

`FixRunner.Fix()` locates the matching rule by stable ID, checks `CanFix()` again to reject stale
issues, and captures both capability-check and repair exceptions as a `FixResult`. Fixes are always
explicit; validation never modifies assets implicitly.

```csharp
var issue = result.Issues.First(candidate => candidate.CanFix);
var currentContext = AssetScanner.Scan(new[] { issue.AssetPath }).Single();
var fixResult = FixRunner.Fix(currentContext, issue);
```

After a successful fix, callers should scan and run validation again. The Editor window performs
this verification automatically for its single-issue **Fix** action.

## Strongly Typed Configuration

`GovernanceProfile` is the project-level `ScriptableObject` configuration asset. The MVP locates
it automatically and permits zero or one profile: no profile means built-in defaults are used,
while multiple profiles stop the scan with an explicit configuration error. Callers such as future
CI integrations can bypass automatic lookup with `AssetScanner.Scan(paths, profile)`.

Rule-specific settings derive from `AssetRuleSettings` and declare their stable `RuleId`. The
profile stores references to the abstract base type, so a third-party rule can add a new strongly
typed settings class without editing `GovernanceProfile` or a central settings registry.

The profile's **Excluded Paths** list skips assets globally before rule execution. Each entry may
identify one asset or a folder; a folder entry matches its descendants using a path-segment boundary,
so `Assets/UI` does not accidentally match `Assets/UIImage`. Separators and trailing slashes are
normalized, paths must start with `Assets` or `Packages`, and entries do not need to exist when the
profile is authored. `AssetScanner` applies exclusions while creating contexts, so built-in and
third-party rules do not need their own exclusion checks. Invalid entries fail the scan with an
explicit configuration message instead of being silently ignored.

The **Whitelist Entries** list provides narrower exceptions. Each entry combines one asset or folder
path with one or more stable rule IDs. Only matching rules are skipped; other rules still inspect the
same resource. Folder matching uses the same normalized path-segment behavior as Excluded Paths.
`RuleRunner` checks the whitelist after rule enablement and before `CanEvaluate()`, so custom rules
receive the behavior without additional code. Invalid whitelist configuration is reported as a
`RuleExecutionStage.Configuration` error, but the rule still executes so malformed exceptions cannot
silently hide real asset violations.

The profile also contains generic **Rule States** entries. Each entry pairs a stable rule ID with an
enabled flag and an optional severity override. Rules without an entry are enabled by default and
keep the severity produced by the rule, so newly installed third-party rules do not require
boilerplate configuration. `RuleRunner` checks the enabled state before calling `CanEvaluate()` and
applies severity overrides when collecting issues, which means every built-in and custom rule
receives both behaviors automatically.

Invalid or duplicate state entries are returned as `RuleExecutionStage.Configuration` errors and do
not terminate the complete validation run. If a severity override is invalid, the original issue is
preserved and the configuration error is reported separately, so a broken project override cannot
hide a real asset violation. Severity settings are only read when a rule actually produces an issue.

```csharp
public sealed class MyRuleSettings : AssetRuleSettings
{
    public override string RuleId => "MY-RULE-001";

    [SerializeField]
    private int maximumValue = 10;

    public int MaximumValue => maximumValue;
}
```

Create the default profile from **Assets > Create > Asset Governance > Governance Profile**. Add
project-relative asset or folder paths to Excluded Paths when all rules should skip them. Use
Whitelist Entries when only named rules should skip a path. Add a rule ID to Rule States only when
its enabled state or severity needs an explicit project override. Create
`UAG-TEX-001` settings from **Assets > Create > Asset Governance > Rule Settings > UI Texture
Mipmap Rule**, add that asset to the profile's Rule Settings list, then configure whether Sprite
textures and/or project path prefixes identify UI textures.

## Built-in Rules

### UAG-NAME-001: Asset Paths Must Not Contain Spaces

This warning applies to every asset type. It reports an issue when any part of the asset path,
including parent folders or the file name, contains a normal space character. The rule is read-only
because automatically renaming assets can affect external tools and workflows.

### UAG-TEX-001: UI Texture Mipmaps Must Be Disabled

This error reports an issue when a UI texture importer has mipmaps enabled. Without project
settings, `TextureImporterType.Sprite` remains the built-in classification. A strongly typed
`UiTextureMipmapsDisabledRuleSettings` asset can keep or disable Sprite classification and add
project-specific UI path prefixes without hard-coding business directories in the rule. This is the
first safely fixable built-in rule: its explicit fix disables `TextureImporter.mipmapEnabled`, saves
and reimports the texture, and is followed by validation again in the Editor window.

### UAG-TEX-002: Texture Read/Write Must Be Disabled

This warning applies to every asset imported through `TextureImporter`. It reports an issue when
`TextureImporter.isReadable` is enabled because retaining a CPU-readable texture copy should be an
explicit project decision. Assets that intentionally require runtime pixel access can be excluded
through a `GovernanceProfile` Whitelist Entry for rule ID `UAG-TEX-002`; the rule does not duplicate
that shared exception mechanism in separate settings. Its explicit safe fix disables Read/Write,
saves and reimports the texture, and the Editor window then validates the current selection again.

## Manual Validation Window

Open `Tools > Asset Governance`, select one or more assets or folders in the Project window, and
click **Scan Selection**. Selected folders are expanded recursively through `AssetScanner`. The
window displays asset issues separately from rule execution errors. Click an asset issue to select
and ping the corresponding asset in the Project window. A confirmed fixable issue also displays a
**Fix** button; successful fixes are followed by a rescan of the current selection.

## Planned Documentation

- Installation
- Configuration
- Writing custom rules
- Batch automatic fixes
- CI integration
