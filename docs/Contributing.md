# Contributing Guide

Thanks for your interest in improving the INPC Field Generator! We aim for a tight feedback loop with high test coverage so changes remain easy to review and ship.

## Prerequisites
- .NET SDK 10 preview (see `global.json`).
- An IDE or editor with C# 14 preview support.
- Avalonia tools if you plan to iterate on the sample UX.

Restore all dependencies once after cloning:
```bash
dotnet restore
```

## Working on the codebase
1. Run the generator unit tests and integration tests frequently:
   ```bash
   dotnet test
   ```
2. Launch the Avalonia sample while developing generator features:
   ```bash
   dotnet run --project samples/ReactiveUiSample
   ```
3. Produce signed packages when touching public surfaces:
   ```bash
   dotnet pack --configuration Release --no-build
   ```
4. Keep documentation in sync. Every behavior change should update the README, attribute reference, or changelog as appropriate.

> We treat warnings as errors. Please fix or suppress diagnostics intentionally rather than ignoring them.

## Style and analyzers
- Nullable reference types are enabled everywhere.
- Use explicit accessibility, expression-bodied members when they improve clarity, and prefer immutable data structures inside the generator.
- Analyzer severities are configured in `.editorconfig`. If you need to suppress a rule, add a justification in code or update the analyzer configuration with consensus.

## Pull request checklist
- [ ] Tests added or updated, including Verify snapshots where applicable.
- [ ] Sample application still compiles and demonstrates the relevant scenario.
- [ ] Documentation updated (`README.md`, `docs/*.md`, or inline XML comments).
- [ ] Changelog entry under the correct heading.
- [ ] `dotnet pack --configuration Release` completes successfully (ensures NuGet metadata stays valid).

## Release process
1. Update `docs/Changelog.md` with a new section for the release.
2. Tag the repository with `vX.Y.Z` (semantic version) and push the tag.
3. The `Release` workflow builds, tests, creates NuGet packages, publishes them when `NUGET_API_KEY` is configured, and creates a GitHub release with artifacts.

By opening a pull request you agree to license your contributions under the project’s MIT license.
