# Inpc Field Generator — Implementation Plan

## 1. Guiding Principles
- Ship in small, reviewable increments backed by automated tests.
- Maintain 100% unit + generator test coverage for `src/` projects (enforced in CI).
- Keep documentation and sample apps evolving alongside features.
- Automate verification (CI) early; avoid manual release steps where tooling is possible.

## 2. Workstream Overview

| Phase | Scope | Primary Outcomes |
| ----- | ----- | ---------------- |
| P0 | Repository bootstrap | Solution structure, project templates, shared props/targets |
| P1 | Generator foundations | Attribute assembly, incremental generator skeleton, diagnostics framework |
| P2 | Feature completeness | Property generation, configuration merging, ReactiveUI integration |
| P3 | Quality gates | Unit/source generator tests, coverage, benchmarks |
| P4 | Sample + docs | Sample app, README, docs set |
| P5 | Packaging & CI | NuGet packaging, GitHub Actions pipelines, release automation |

Phases overlap where practical but respect dependency ordering.

## 3. Detailed Task Breakdown

### 3.1 Phase P0 — Bootstrap (ETA 1–2 days)
- Create `InpcFieldGenerator.sln` with solution folders (`src`, `tests`, `samples`, `docs`, `.github`).
- Author `Directory.Build.props`/`Directory.Build.targets` centralizing SDK version (`net10.0`), language version (`preview`/`14.0`), analysis rules, and code style.
- Configure global.json to lock tooling (SDK supporting C# 14 preview).
- Set up `.editorconfig` aligning analyzer severities and code style used by generator.

### 3.2 Phase P1 — Generator Foundations (ETA 3–4 days)
- **Attributes Assembly (`InpcFieldGenerator.Abstractions`)**
  - Implement `[ReactiveField]`, `[ReactiveViewModel]`, `[ReactiveFieldOption]`.
  - Provide XML docs and analyzer-friendly assembly metadata.
  - Add unit tests validating attribute defaults.
- **Incremental Generator Project (`InpcFieldGenerator`)**
  - Reference `Microsoft.CodeAnalysis.CSharp` packages.
  - Implement `InpcFieldGenerator` class with `Initialize` method stub.
  - Add configuration models (`ReactiveFieldOptions`, `PropertyModel`).
  - Implement diagnostics catalog (`DiagnosticDescriptors` static class).
  - Hook post-initialization task to embed attribute source for consumers without runtime assembly reference fallback.

### 3.3 Phase P2 — Feature Completeness (ETA 5–6 days)
- **Syntax Providers**
  - Collect `partial` property declarations with `[ReactiveField]`.
  - Gather containing type metadata, attribute arguments, and class-level defaults.
- **Configuration Merge**
  - Merge assembly, class, and property-level options with precedence rules.
  - Validate `AlsoNotify` property names at compile time using semantic model.
- **Code Generation**
  - Implement emitter producing deterministic partial code using `IndentedStringBuilder`.
  - Support generics, nullable reference types, accessibility modifiers.
  - Respect developer-defined getter/setter additions around the `field` keyword.
- **ReactiveUI Integration**
  - Ensure generator locates `RaisePropertyChanged`/`RaisePropertyChanging`.
  - Emit diagnostics when required members missing, with fix suggestions.
  - Provide optional partial method hooks `On<Property>Changing/Changed`.
- **Incremental Performance**
  - Validate caching behavior; avoid reprocessing unaffected syntax.
  - Add logging guard (for debug builds) using `GeneratorExecutionContext.AnalyzerConfigOptions`.

### 3.4 Phase P3 — Quality Gates (ETA 4–5 days)
- **Unit & Generator Tests**
  - Use `xUnit`, `FluentAssertions`, `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing`.
  - Snapshot tests via `Verify` for generated output covering:
    - Basic property generation with `field`.
    - Equality guard on/off.
    - `AlsoNotify` cross-property triggers.
    - Missing method diagnostics.
  - Analyzer tests verifying diagnostic placements and severities.
- **Coverage**
  - Integrate `coverlet.collector`; enforce 100% coverage via Azure/`dotnet test --collect:"XPlat Code Coverage"`.
  - Add coverage report generation in CI (use ReportGenerator for HTML summary artifact).
- **Benchmarks (Optional but recommended)**
  - Initialize `BenchmarkDotNet` project to monitor generator throughput.

### 3.5 Phase P4 — Sample Application & Documentation (ETA 3–4 days)
- **Sample App (`samples/ReactiveUiSample`)**
  - Choose WPF (.NET 10) for desktop demonstration (Avalonia optional for cross-platform).
  - Implement simple MVVM view model using generator and data binding.
  - Add README section referencing sample scenario.
- **Documentation**
  - Author `README.md` with quick start, installation via NuGet, attribute reference, troubleshooting.
  - Create `docs/AttributeReference.md`, `docs/Contributing.md`, `docs/Changelog.md`.
  - Document coverage instructions and analyzer suppression guidelines.

### 3.6 Phase P5 — Packaging & CI (ETA 2–3 days)
- **NuGet Packaging**
  - Configure `PackageId`, metadata, `buildTransitive` props/targets.
  - Include icon, license, README in package.
  - Validate packaging via `dotnet pack` and `nuget pack` tests.
- **GitHub Actions**
  - `ci.yml`: triggers on PR/main, matrix for `os: [ubuntu-latest, windows-latest]`.
    - Steps: checkout, setup-dotnet (preview channel), restore, build, test (with coverage), pack, upload artifacts.
  - `release.yml`: manual dispatch or tag `v*`.
    - Steps: build/test/pack, push to NuGet (using secrets), create GitHub Release with changelog, attach packages.
- **Versioning**
  - Employ GitVersion or `dotnet minver` for semantic version automation.
  - Document release steps (tagging, changelog update).

## 4. Cross-Cutting Concerns

- **Coding Standards**: Enable nullable reference types, treat warnings as errors in generator projects.
- **Static Analysis**: Integrate `Roslynator.Analyzers` and `IDisposable` analyzers; document suppression workflow.
- **Localization**: Centralize diagnostic messages for future localization (resource file).
- **Threading**: No shared mutable state; prefer `ImmutableArray`/`ImmutableDictionary`.
- **Developer Tooling**: Provide `dotnet format` config and hook into CI as optional step.

## 5. Milestone Acceptance Criteria
- **M1 (P0–P1)**: Solution builds; generator emits no-ops but registers attributes; initial tests green.
- **M2 (P2)**: Generator outputs functional property code; diagnostics validated via automated tests.
- **M3 (P3)**: Test suite covers all branches/lines; coverage report produced in CI.
- **M4 (P4)**: Sample app demonstrates generator; documentation ready for external users.
- **M5 (P5)**: CI pipelines succeed; NuGet artifacts publishable; release workflow verified in dry run.

## 6. Tooling & Dependencies
- **SDK**: .NET 10 preview (global.json pinned).
- **Packages**: `Microsoft.CodeAnalysis.Analyzers`, `Microsoft.CodeAnalysis.CSharp.Workspaces`, `ReactiveUI`, `Verify.SourceGenerators`, `FluentAssertions`, `coverlet.collector`, `ReportGenerator`.
- **CI**: GitHub Actions with `actions/setup-dotnet@v4`.
- **Code Style**: `.editorconfig` aligning with .NET Runtime conventions.

## 7. Risk Mitigation & Contingencies
- **C# 14 Instability**: Track SDK updates, include integration test verifying minimal required SDK; document preview caveats.
- **ReactiveUI API Changes**: Abstract method invocations through small helper to ease updates; pin dependency version.
- **Coverage Flakiness**: Run tests in parallel-friendly manner; disable parallelization for generator tests if necessary.
- **Release Secrets**: Store `NUGET_API_KEY`, `GITHUB_TOKEN` securely; add instructions for maintainers to rotate.

## 8. Communication & Review
- Use GitHub Projects issue board aligned with phases.
- Enforce PR template requiring test evidence and documentation updates.
- Weekly checkpoints verifying milestone progress against acceptance criteria.

## 9. Completion Definition
- Generator emits stable code with required features.
- All tests + sample build succeed locally and in CI.
- Publication pipeline pushes packages and release notes automatically.
- Documentation (README + docs) accurately reflects features and usage.
- Version `1.0.0` tagged and released with artifacts.

This plan sequences the work needed to deliver a production-ready INPC field generator aligned with the design specification.
