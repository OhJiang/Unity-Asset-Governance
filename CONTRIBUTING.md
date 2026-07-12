# Contributing

Contributions are welcome while the project is in early development. Keep changes small, focused,
and covered by EditMode tests.

## Development Environment

- Unity 6000.5 or newer
- Clone the repository and open its root as a Unity project
- The package source is under `Packages/com.bliblob23.asset-governance`

## Development Rules

- Keep the package Editor-only unless a runtime API is explicitly approved.
- New rules must implement a public framework contract and use a stable, globally unique rule ID.
- Do not add rules to a central hard-coded registry; public rules must remain discoverable through
  Unity `TypeCache`.
- Put project-specific thresholds, paths, and exceptions in strongly typed `ScriptableObject`
  settings rather than hard-coding them in rule implementations.
- `Evaluate()` must be read-only. Resource modifications belong only in explicit
  `IFixableAssetRule.Fix()` implementations.
- Add EditMode tests for passing, failing, configuration, and fix behavior relevant to the change.

## Validation

Before opening a pull request:

1. Open **Window > General > Test Runner**.
2. Run the complete **EditMode** test suite.
3. Confirm that the Console has no compilation errors.
4. Review `git diff --check` and ensure unrelated files are not included.

The package currently targets Unity `6000.5.2f1` for its full local regression suite.

## Commits And Pull Requests

- Use a focused commit title that describes the behavior changed.
- Explain the motivation, implementation, and tests in the commit or pull request body.
- Call out public API or serialized configuration changes explicitly.
- Do not combine unrelated refactors with a feature or bug fix.

By participating, you agree to follow the repository's [Code of Conduct](CODE_OF_CONDUCT.md).
