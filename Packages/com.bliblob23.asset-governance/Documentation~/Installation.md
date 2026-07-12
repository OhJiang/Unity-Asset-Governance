# Installation

## Requirements

- Unity 6000.5 or newer
- Editor usage only

The package does not add assemblies to Player builds.

## Install From Git URL

Open **Window > Package Management > Package Manager**, click **+**, choose **Install package from
git URL**, and enter:

```text
https://github.com/OhJiang/Unity-Asset-Governance.git?path=/Packages/com.bliblob23.asset-governance
```

Unity records the dependency in the consuming project's `Packages/manifest.json`.

## Install From Local Disk

For package development or an offline checkout:

1. Open Package Manager.
2. Click **+ > Install package from disk**.
3. Select `Packages/com.bliblob23.asset-governance/package.json` from the checkout.

## Verify The Installation

After Unity finishes compiling:

1. Confirm that **Tools > Asset Governance** is available.
2. Create **Assets > Create > Asset Governance > Governance Profile**.
3. Open the validation window and click **Scan Project Assets**.

A project may have zero or one Governance Profile. Zero profiles use built-in defaults; multiple
profiles produce an explicit configuration error.

## Import The Custom Rule Sample

Select Unity Asset Governance in Package Manager, open the **Samples** tab, and import **Custom Rule
and Strongly Typed Settings**. The imported sample includes an independent Editor assembly, its own
`ScriptableObject` settings type, and EditMode tests.

## Remove The Package

Remove Unity Asset Governance from Package Manager. Governance Profile and rule settings assets
created under the consuming project's `Assets` directory are project assets and are not deleted
automatically.
