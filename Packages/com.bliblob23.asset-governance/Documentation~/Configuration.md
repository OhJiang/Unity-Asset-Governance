# Configuration

## Governance Profile

Create **Assets > Create > Asset Governance > Governance Profile**. The current release supports
zero or one profile per project. Without a profile, built-in rules use their safe defaults. More than
one profile stops scanning with an explicit error so configuration does not become ambiguous.

## Excluded Paths

**Excluded Paths** skip an asset or folder before any rule executes.

- Paths must start with `Assets` or `Packages`.
- Both slash styles are normalized.
- A folder entry excludes its descendants using path-segment matching.
- Use exclusions only when no governance rule should inspect the path.

## Rule Whitelists

Each **Whitelist Entry** contains an asset or folder path plus one or more stable rule IDs. Only the
listed rules skip the matching path; other rules still evaluate it.

Prefer a whitelist over a global exclusion when an asset intentionally violates one rule, such as a
texture that requires runtime CPU access and therefore cannot satisfy `UAG-TEX-002`.

## Rule States

A **Rule State** can:

- Enable or disable one stable rule ID.
- Override the severity produced by that rule.

Rules without an entry remain enabled and keep their default severity. This default allows newly
installed third-party rules to work without adding boilerplate state entries.

## Strongly Typed Rule Settings

Rule-specific settings derive from `AssetRuleSettings` and are stored in the profile's **Rule
Settings** list. Each settings asset declares the stable rule ID it belongs to.

Create built-in settings through **Assets > Create > Asset Governance > Rule Settings**. Third-party
packages can add their own creation menu and strongly typed fields without changing
`GovernanceProfile`.

The framework reports missing references, duplicate settings for one rule ID, and settings type
mismatches as explicit configuration errors.

## Recommended Setup Order

1. Create one Governance Profile.
2. Configure global exclusions sparingly.
3. Add rule-specific settings for business paths and thresholds.
4. Add narrow whitelist entries for intentional exceptions.
5. Add Rule States only for explicit enablement or severity policy changes.
6. Run **Scan Project Assets** and review configuration errors before fixing asset issues.
