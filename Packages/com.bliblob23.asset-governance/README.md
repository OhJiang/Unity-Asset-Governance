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
- Minimal Editor window for scanning selected assets or folders and locating reported assets
- Strongly typed `ScriptableObject` rule settings through `GovernanceProfile`
- Project-wide rule enable/disable states and optional severity overrides enforced centrally by `RuleRunner`
- Global asset and folder exclusions applied centrally by `AssetScanner` before rules execute
- Automatic discovery of the single project default profile, with explicit duplicate-profile errors
- Configurable UI texture classification by importer type and project path prefixes

## Quick Configuration

1. Create **Assets > Create > Asset Governance > Governance Profile**. The MVP supports one profile per project.
2. Add asset or folder paths to **Excluded Paths** when they must be skipped globally. Paths must start with `Assets` or `Packages`; folders exclude all descendants.
3. Add a rule ID to **Rule States** when a rule must be explicitly enabled, disabled, or assigned a project-specific severity. Rules without a state entry remain enabled and keep their original severity by default.
4. Create **Assets > Create > Asset Governance > Rule Settings > UI Texture Mipmap Rule** and add it to **Rule Settings**.
5. Keep Sprite classification enabled, add project-specific UI path prefixes, or combine both.

When no profile or no `UAG-TEX-001` settings exist, the built-in rule keeps its safe default of treating Sprite textures as UI textures. Custom rules can derive their own settings from `AssetRuleSettings`; the core profile does not need to know concrete third-party settings types.

## Roadmap

1. Common profile options: whitelist
2. Automatic fixes
3. CI integration

## License

MIT
