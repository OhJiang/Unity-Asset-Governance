# Custom Rule And Strongly Typed Settings Example

This sample shows how a third-party Editor assembly can extend Unity Asset Governance without
modifying the package source or a central rule list.

The sample contains:

- `TextureNamePrefixRule`, a public `IAssetRule` implementation discovered automatically through
  Unity `TypeCache`.
- `TextureNamePrefixRuleSettings`, a rule-owned `ScriptableObject` derived from
  `AssetRuleSettings`.
- An independent Editor assembly definition that references only `UnityAssetGovernance.Editor`.
- EditMode tests covering discovery, missing configuration, violations, and compliant assets.

## Try The Sample

1. Import **Custom Rule and Strongly Typed Settings** from the Package Manager Samples tab.
2. Create the settings asset from **Assets > Create > Asset Governance > Samples > Texture Name
   Prefix Rule**.
3. Configure the governed texture folder and required file name prefix.
4. Create or select the project's **Governance Profile**, then add the new settings asset to its
   **Rule Settings** list.
5. Open **Tools > Asset Governance** and scan the configured folder or all project assets.

The rule is inactive when its settings are not present in the profile. Once configured, textures
under the selected path must start with the configured prefix. For example, with `T_` as the prefix,
`T_Hero.png` passes and `Hero.png` reports `SAMPLE-TEX-001`.

## Extension Points Demonstrated

- A stable third-party rule ID.
- Automatic rule discovery from an independent assembly.
- Strongly typed project configuration without changing `GovernanceProfile`.
- Central rule enablement, severity override, exclusions, and whitelist support supplied by
  `RuleRunner` and `AssetScanner` without custom rule code.
