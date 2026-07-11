# Unity Asset Governance

Unity Asset Governance is an extensible Unity Editor framework for asset validation and governance.

## Status

Early development. Public APIs are not stable.

## Requirements

- Unity 6000.5 or newer

## Current Features

- Embedded UPM package structure
- Editor-only assembly
- EditMode test assembly
- Package smoke test
- Public, implementation-agnostic asset rule contract
- Immutable rule metadata, asset context, and validation issue models
- Automatic rule discovery through Unity `TypeCache`
- Rule construction, descriptor, duplicate ID, and deterministic ordering validation
- Recursive asset and folder scanning with duplicate removal and deterministic ordering
- Synchronous rule execution with exception isolation and deterministic results
- Built-in `UAG-NAME-001` rule for asset paths and file names containing spaces
- Built-in `UAG-TEX-001` rule requiring configured UI texture mipmaps to be disabled
- Built-in `UAG-TEX-002` rule requiring texture Read/Write to be disabled unless whitelisted
- Built-in `UAG-TEX-003` rule enforcing configurable Texture Max Size limits
- Built-in `UAG-TEX-004` rule requiring Normal Map sRGB sampling to be disabled
- Built-in `UAG-MODEL-001` rule enforcing configurable Model Scale Factor values
- Built-in `UAG-MODEL-002` rule requiring model Read/Write to be disabled unless whitelisted
- Built-in `UAG-AUDIO-001` rule preventing configured short audio from using Streaming
- Minimal Editor window for scanning selected assets or folders and locating reported assets
- Strongly typed `ScriptableObject` rule settings through `GovernanceProfile`
- Project-wide rule enable/disable states and optional severity overrides enforced centrally by `RuleRunner`
- Global asset and folder exclusions applied centrally by `AssetScanner` before rules execute
- Rule-specific asset and folder whitelist entries enforced centrally by `RuleRunner`
- Extensible `IFixableAssetRule` contract and exception-isolated `FixRunner` for explicit safe fixes
- Single-issue fix buttons with automatic rescan in the Editor window
- Safe automatic Mipmap disabling for `UAG-TEX-001`
- Safe automatic Read/Write disabling for `UAG-TEX-002`
- Explicit Texture Max Size correction for `UAG-TEX-003`
- Safe automatic sRGB sampling disabling for `UAG-TEX-004`
- Safe automatic model Read/Write disabling for `UAG-MODEL-002`
- Configurable non-Streaming correction for `UAG-AUDIO-001`
- Automatic discovery of the single project default profile, with explicit duplicate-profile errors
- Configurable UI texture classification by importer type and project path prefixes

## Quick Configuration

1. Create **Assets > Create > Asset Governance > Governance Profile**. The MVP supports one profile per project.
2. Add asset or folder paths to **Excluded Paths** when they must be skipped globally. Paths must start with `Assets` or `Packages`; folders exclude all descendants.
3. Add a path and one or more stable rule IDs to **Whitelist Entries** when only those rules should skip that asset or folder.
4. Add a rule ID to **Rule States** when a rule must be explicitly enabled, disabled, or assigned a project-specific severity. Rules without a state entry remain enabled and keep their original severity by default.
5. Create **Assets > Create > Asset Governance > Rule Settings > UI Texture Mipmap Rule** and add it to **Rule Settings**.
6. Keep Sprite classification enabled, add project-specific UI path prefixes, or combine both.
7. Create **Assets > Create > Asset Governance > Rule Settings > Texture Max Size Rule** and add it to **Rule Settings** when the default limit of 2048 is not suitable.
8. Set a project default and optional asset or folder path overrides. When multiple paths match, the most specific path wins.
9. Create **Assets > Create > Asset Governance > Rule Settings > Model Scale Factor Rule** and add it to **Rule Settings** when the default expected value of 1 is not suitable.
10. Set a project default and optional model or folder path overrides. Scale Factor findings are intentionally read-only because changing them can alter scene dimensions.
11. Create **Assets > Create > Asset Governance > Rule Settings > Short Audio Streaming Rule** and add it to **Rule Settings**.
12. Add project-specific short-audio path prefixes and choose the non-Streaming Load Type used by explicit fixes. No short-audio business path is built into the package.

When no profile or no `UAG-TEX-001` settings exist, the built-in rule keeps its safe default of treating Sprite textures as UI textures. `UAG-TEX-002` applies to every `TextureImporter`; add an asset or folder path with rule ID `UAG-TEX-002` to Whitelist Entries only when runtime CPU texture access is intentional. `UAG-TEX-003` applies a default Max Size limit of 2048 unless strongly typed settings provide a different default or path-specific override. `UAG-TEX-004` applies only to textures explicitly imported as Normal Maps; intentional exceptions can reuse the shared rule whitelist. `UAG-MODEL-001` expects Model Scale Factor 1 by default and supports strongly typed default and path-specific values. `UAG-MODEL-002` applies to every `ModelImporter`; whitelist only models that intentionally require runtime CPU mesh access. `UAG-AUDIO-001` runs only when strongly typed settings classify a clip by project path, and fixes Streaming clips to the configured non-Streaming Load Type. Custom rules can derive their own settings from `AssetRuleSettings`; the core profile does not need to know concrete third-party settings types.

## Roadmap

1. Additional built-in rules
2. Selected-issue batch fixes with confirmation
3. CI integration

## License

MIT
