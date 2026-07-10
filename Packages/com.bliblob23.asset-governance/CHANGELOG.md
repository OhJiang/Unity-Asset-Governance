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
