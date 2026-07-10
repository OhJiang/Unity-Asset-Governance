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
- Minimal Editor window for scanning selected assets or folders and locating reported assets

## Roadmap

1. UI Texture Mipmap probe rule
2. ScriptableObject configuration
3. Automatic fixes
4. CI integration

## License

MIT
