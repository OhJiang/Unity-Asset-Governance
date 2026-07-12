# Unity Asset Governance

Unity Asset Governance is an extensible Unity Editor framework for configuration-driven asset
validation, project governance, and explicit safe fixes.

The repository contains an embedded UPM package at:

```text
Packages/com.bliblob23.asset-governance
```

## Status

`v0.1.0` is the first public MVP release. The package supports manual project scans and explicit
safe fixes. Public APIs may still change before `1.0.0`.

## Requirements

- Unity 6000.5 or newer
- Editor usage only; the package does not add runtime Player code

## Install From Git

In Unity, open **Window > Package Management > Package Manager**, click **+**, choose
**Install package from git URL**, and enter:

```text
https://github.com/OhJiang/Unity-Asset-Governance.git?path=/Packages/com.bliblob23.asset-governance
```

For local development, choose **Install package from disk** and select the package's `package.json`.

## First Scan

1. Create **Assets > Create > Asset Governance > Governance Profile**.
2. Open **Tools > Asset Governance**.
3. Use **Scan Selection** for selected assets or folders, or **Scan Project Assets** for the complete
   project `Assets` tree.
4. Review issues, locate assets, and run explicit fixes only where the window reports a fixable issue.

## Documentation

- [Package overview](Packages/com.bliblob23.asset-governance/README.md)
- [Installation](Packages/com.bliblob23.asset-governance/Documentation~/Installation.md)
- [Configuration](Packages/com.bliblob23.asset-governance/Documentation~/Configuration.md)
- [Writing a custom rule](Packages/com.bliblob23.asset-governance/Documentation~/WritingARule.md)
- [Changelog](Packages/com.bliblob23.asset-governance/CHANGELOG.md)
- [Contributing](CONTRIBUTING.md)

## License

MIT. See [LICENSE.md](LICENSE.md).
