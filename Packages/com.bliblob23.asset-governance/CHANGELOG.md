# Changelog

All notable changes to this package will be documented in this file.

## [Unreleased]

### Added

- Initial UPM package structure.
- Editor-only assembly.
- Editor test assembly.
- Package smoke test.
- Extensible `IAssetRule` contract.
- Immutable rule descriptor, asset context, severity, and validation issue models.
- Automatic `IAssetRule` discovery through Unity `TypeCache`.
- Validation for rule construction, descriptors, duplicate IDs, and deterministic ordering.
- Recursive asset and folder scanning with duplicate removal and deterministic ordering.
- Synchronous rule execution with exception isolation and deterministic result ordering.
- Built-in `UAG-NAME-001` rule for detecting spaces in asset paths and file names.
- Built-in `UAG-TEX-001` rule requiring Sprite texture mipmaps to be disabled.
- Minimal Editor window for scanning selected assets and folders, displaying results, and locating assets.
- Strongly typed `AssetRuleSettings` extension point and project-level `GovernanceProfile`.
- Automatic unique default profile lookup plus explicit profile injection through `AssetScanner`.
- Configurable Sprite and path-prefix classification for the built-in `UAG-TEX-001` rule.
- Project-wide rule enable and disable states with enabled-by-default behavior.
- Central rule-state enforcement and isolated configuration-stage execution errors in `RuleRunner`.
- Optional project-wide severity overrides applied centrally while preserving original issues when configuration is invalid.
- Global asset and folder exclusions with normalized, segment-aware matching applied by `AssetScanner`.
- Rule-specific asset and folder whitelist entries applied before `CanEvaluate()` without hiding violations when configuration is invalid.
- Extensible `IFixableAssetRule` contract with framework-confirmed `ValidationIssue.CanFix` state.
- Exception-isolated `FixRunner` with stale-issue capability checks and structured fix results.
- Safe automatic Mipmap disabling for the built-in `UAG-TEX-001` rule.
- Built-in `UAG-TEX-002` rule with whitelist support and safe automatic Texture Read/Write disabling.
- Built-in `UAG-TEX-003` rule with strongly typed default and longest-path Texture Max Size limits plus explicit correction.
- Single-issue Editor window fixes followed by automatic selection rescanning.
- Public rule type filtering so non-public test and implementation helpers are ignored during `TypeCache` discovery.
