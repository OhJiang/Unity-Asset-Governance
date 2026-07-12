# v0.1.0 Release Checklist

Use this checklist when the release candidate is ready to become the public `v0.1.0` tag.

## Package Content

- [ ] `package.json` name is `com.bliblob23.asset-governance` and version is `0.1.0`.
- [ ] Package description matches implemented features and does not advertise planned CI features.
- [ ] README, installation, configuration, custom rule, changelog, and license files are present.
- [ ] The Package Manager sample path exists and imports without compilation errors.
- [ ] No runtime assembly or Player dependency has been added.

## Validation

- [ ] Open the repository with the supported Unity version.
- [ ] Run the complete package EditMode test suite.
- [ ] Import **Custom Rule and Strongly Typed Settings** and run its EditMode tests.
- [ ] Install the package from the Git URL in a clean Unity project after the release commit is pushed.
- [ ] Create one Governance Profile and complete both a selection scan and project `Assets` scan.
- [ ] Confirm a single fix and a selected-issue batch fix both rescan the original scope.
- [ ] Confirm the Console contains no compilation errors or unexpected package warnings.

## Version And Git

- [ ] Move completed entries from `[Unreleased]` to `[0.1.0] - YYYY-MM-DD` in `CHANGELOG.md`.
- [ ] Ensure the release commit contains only intended files and the working tree is clean.
- [ ] Create the annotated tag `v0.1.0` on the verified release commit.
- [ ] Push the release branch and tag.
- [ ] Create GitHub release notes from the `CHANGELOG.md` entry.

Do not mark the changelog version as released or create the tag until the pushed commit has been
installed successfully in a clean Unity project.
