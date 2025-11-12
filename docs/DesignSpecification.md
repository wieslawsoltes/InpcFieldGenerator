# INPC Field Generator — Design Specification

## 1. Product Overview

The **INPC Field Generator** is an incremental Roslyn source generator that produces high-quality `INotifyPropertyChanged` (INPC) plumbing for ReactiveUI-based view models. The generator targets C# 14 and .NET 10, embracing the new `field` keyword for partial properties to minimize boilerplate and improve readability. Consumers annotate `partial` classes and declare `partial` properties whose setters leverage the generated `field` keyword. The generator injects backing fields, change notification logic, and optional cross-property notifications, ensuring idiomatic integration with ReactiveUI (`RaisePropertyChanged`, `WhenAnyValue`, etc.).

The deliverables include:

- A NuGet-packable core source generator library with analyzers, diagnostics, and MSBuild integration.
- An accompanying attribute/abstractions package for consumer projects.
- A sample application demonstrating end-to-end usage.
- Comprehensive automated testing (100% coverage goal) and GitHub Actions pipelines for CI and release automation.

## 2. Goals and Non-Goals

**Goals**
- Automate INPC implementation for ReactiveUI view models using annotated `partial` properties.
- Ensure deterministic, incremental code generation with minimal rebuild overhead.
- Provide granular diagnostics, configuration, and discoverability through analyzers and documentation.
- Offer a first-class developer experience: IntelliSense-friendly generated code, optional customization hooks, and editorconfig-driven behavior tweaks.
- Deliver production-ready distribution assets (NuGet, CI workflows, sample).

**Non-Goals**
- Implement a runtime reflection-based solution (the generator works at compile time only).
- Support non-ReactiveUI frameworks or legacy C# versions lacking partial properties with `field`.
- Generate additional features unrelated to INPC (e.g., command generation, validation).
- Persist tooling beyond source generation (e.g., no VS extension).

## 3. Target Scenarios

1. **ReactiveUI View Model**: Developers author view models as `partial` classes, annotate properties with `[ReactiveField]`, and rely on auto-generated change notification.
2. **Cross-Property Notification**: A calculated property re-evaluates when a source property changes through `AlsoNotify` metadata.
3. **Computed Dependencies**: The generator reuses ReactiveUI's `RaisePropertyChanging/Changed` conventions and supports optional `PropertyChanging`.
4. **MVVM Sample**: A sample (WPF or Avalonia) demonstrates binding, validation, and diagnostics consumption.

## 4. Functional Requirements

### 4.1 Attribute Surface

- `[ReactiveField]`: Applied to `partial` property declarations. Supports named parameters:
  - `string? PropertyName` (override property name when derived from field/JSON).
  - `string? ViewModelMember` (custom notification method override; default `RaisePropertyChanged`).
  - `bool NotifyOnChanging` (emit `RaisePropertyChanging` before value assignment).
  - `string[] AlsoNotify` (additional property names to notify).
  - `bool GenerateEqualityCheck` (enable/disable equality guard).
- `[ReactiveViewModel]`: Placed on `partial` classes to opt-in once. Allows per-class defaults (e.g., default `NotifyOnChanging`).
- `[ReactiveFieldOption]`: Optional assembly-level attribute for global defaults.

Attributes live in a minimal runtime-friendly assembly referenced by consumers.

### 4.2 Generated Output

- For each targeted property, the generator creates a partial definition supplying:
  - A private backing field.
  - A `partial` property implementation using the `field` keyword in setter.
  - Optional equality guard leveraging `EqualityComparer<T>.Default`.
  - Calls into `RaisePropertyChanging` (if enabled) and `RaisePropertyChanged`.
  - Additional notifications for `AlsoNotify`.

Example (simplified):

```csharp
partial class PersonViewModel : ReactiveObject
{
    [ReactiveField]
    public partial string FirstName { get; set; }
}
```

Generated partial snippet:

```csharp
partial class PersonViewModel
{
    private string _firstName;

    public partial string FirstName
    {
        get => _firstName;
        set
        {
            var oldValue = _firstName;
            if (EqualityComparer<string>.Default.Equals(oldValue, value))
                return;
            RaisePropertyChanging(nameof(FirstName));
            field = value;
            RaisePropertyChanged(nameof(FirstName));
        }
    }
}
```

The generator is resilient to pre-existing partial members, only filling in missing parts. The `field` keyword updates the backing store while preserving developer-defined setter logic around it.

### 4.3 ReactiveUI Integration

- Generated notifications rely on `RaisePropertyChanging` / `RaisePropertyChanged`. If those methods do not exist, the generator emits diagnostics.
- Supports classes inheriting from `ReactiveObject` or implementing required methods via partial definitions.
- Optionally supports derived classes overriding hooks through `protected virtual` partial methods the generator can invoke (`On<PropName>Changing/Changed`).

### 4.4 Incremental Generation Flow

1. **Syntax Provider**: Collect property declarations marked with `[ReactiveField]` plus their containing type symbol.
2. **Semantic Transformation**: Bind attributes, compute effective configuration (class-level + assembly-level + property-level).
3. **Validation**: Ensure declarations meet prerequisites (partial property, accessible setter, correct contaning type). Emit diagnostics (error/warning/info) as needed.
4. **Code Emission**: Generate deterministic hint names per property/class using `IncrementalGeneratorPostInitializationContext` (preload attribute source) and `SourceOutput`.
5. **Caching**: Leverage incremental pipelines to recompute only affected nodes upon changes.

### 4.5 Diagnostics and Telemetry

- Diagnostic IDs `INPCFG001`–`INPCFG010` covering missing `partial`, unsupported types, invalid `AlsoNotify` references, missing notification methods, etc.
- Analyzer ensures attributes are applied correctly and surfaces fix suggestions where possible.
- Logging via `GeneratorExecutionContext.ReportDiagnostic` for misconfiguration.

## 5. Non-Functional Requirements

- **Performance**: Must not significantly slow down compile times; uses incremental APIs, caches metadata, and avoids large string concatenations (use `StringBuilder`/`SourceText` pooling).
- **Thread Safety**: Stateless transformations; avoid shared mutable state.
- **Diagnostics Quality**: Provide actionable messages and documentation links.
- **Compatibility**: The runtime attribute assembly targets `netstandard2.0` to support broader consumption while generator uses `netstandard2.0` plus `Microsoft.CodeAnalysis.CSharp`.
- **Tooling**: Provide analyzers to prompt enabling `C# 14` and `field` keyword usage, supplied via `LangVersion` check.

## 6. Architecture

- **Projects**
  - `src/InpcFieldGenerator`: Incremental generator + analyzers.
  - `src/InpcFieldGenerator.Abstractions`: Attribute definitions and helper interfaces; distributed as dependency.
  - `src/InpcFieldGenerator.Benchmarks`: Optional performance bench (excluded from coverage).
  - `samples/ReactiveUiSample`: Demo app referencing NuGet-style packages via project references.
  - `tests/InpcFieldGenerator.Tests`: Unit tests for generator (using `Verify`, `Microsoft.CodeAnalysis.Testing` harness).
  - `tests/InpcFieldGenerator.IntegrationTests`: Builds sample scenarios ensuring generator works end-to-end.

- **Namespace Layout**
  - `InpcFieldGenerator`
    - `IncrementalGenerators.InpcGenerator`
    - `Configuration` (option merging)
    - `Model` (per-property metadata)
    - `Rendering` (code emission via templates/indented text writer)
    - `Diagnostics`

- **Template Strategy**
  - Use hand-crafted C# emitter with formatting helpers.
  - Provide `PropertyEmitter` generating setter/getter, `NotificationEmitter` for RAII style.

- **Configuration Inputs**
  - `EditorConfigOptionsProvider` for features such as default equality guard or naming patterns.
  - Additional files (JSON or `.props`) for advanced configuration (optional future scope).

## 7. External Interfaces

- **NuGet Packaging**: Multi-target `netstandard2.0` for generator, include analyzer assets.
- **MSBuild**: Provide `buildTransitive` targets that auto-add `InpcFieldGenerator.Abstractions` and configure `LangVersion=preview` or 14.
- **Documentation**: README detailing installation, attributes, limitations, and sample code.
- **GitHub Actions**: Workflows for PR validation and release (pack + push + GitHub Release).

## 8. Testing Strategy

- **Unit Tests**: Validate attribute parsing, configuration merging, and diagnostics. Use xUnit + FluentAssertions.
- **Source Generator Testing**: Use `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing` plus `Verify` snapshots to assert generated code.
- **Golden Master**: Snapshots of generated code for representative inputs (with `field` usage).
- **Integration Tests**: Build the sample app with the generator in CI to ensure compatibility.
- **Coverage**: Enforce 100% line + branch coverage for generator and abstractions, measured via `coverlet.collector`. Fail CI if coverage < 100%.

## 9. Documentation Deliverables

- **README** (root): Overview, quick start, attribute reference, customization, troubleshooting.
- **docs/**:
  - `DesignSpecification.md` (this document).
  - `ImplementationPlan.md` (detailed execution roadmap).
  - `AttributeReference.md` (detailed attribute documentation) — to be authored during implementation.
  - `Contributing.md` (build/testing guidelines).

## 10. Deployment & Distribution

- **CI Build Pipeline**
  - Trigger: PRs & pushes to main.
  - Jobs: Restore, build, run tests with coverage, verify sample builds, produce artifacts.
- **Release Pipeline**
  - Manual dispatch or tag.
  - Steps: Build, test, pack NuGet, publish to GitHub Packages/NuGet.org, create GitHub Release with changelog and artifacts.
- **Versioning**: Semantic versioning, automated via Git tags.
- **Artifacts**: NuGet `.nupkg` for generator and abstractions, zipped sample, coverage report.

## 11. Risk Assessment & Mitigation

- **C# 14 Adoption**: Since the `field` keyword is new, ensure `LangVersion` is validated; provide fallback diagnostic instructing users to enable preview features.
- **ReactiveUI Dependency Changes**: Abstract interactions to ReactivUI's stable API (`RaisePropertyChanged`) to minimize breaking changes.
- **Complex Notify Graphs**: Validate `AlsoNotify` property names at compile time to avoid runtime failures.
- **Performance Regressions**: Add benchmarks and incremental generator tests ensuring performance budgets remain acceptable.

## 12. Future Enhancements

- Support for `NotifyDataErrorInfo`.
- Command generation tied to property updates.
- Customizable templates via additional files.
- Analyzer-driven code fixes to convert manual properties into attribute-based ones.

---

This specification establishes the foundation for building a production-grade INPC field generator aligned with ReactiveUI best practices, modern C# language features, and robust engineering workflows.
