# INPC Field Generator

An incremental Roslyn source generator that adds high-quality `INotifyPropertyChanged` plumbing to ReactiveUI view models using the C# 14 `field` keyword. Annotate your partial properties with `[ReactiveField]` and let the generator take care of backing fields, change notifications, and cross-property updates.

## Why this generator?
- Works incrementally with C# 14 partial properties and the `field` keyword.
- Integrates with ReactiveUI notification patterns (`RaisePropertyChanged`, `RaisePropertyChanging`).
- Provides granular configuration through attributes and assembly defaults.
- Ships with an Avalonia desktop sample that demonstrates generator output end-to-end.

## Installation
Install both the analyzer package and the attribute library into your solution:

```bash
dotnet add package InpcFieldGenerator --prerelease
dotnet add package InpcFieldGenerator.Abstractions --prerelease
```

The generator package sets `LangVersion=preview` automatically through its `buildTransitive` props. If you already control the language version, set `InpcFieldGeneratorSetLangVersion=false` in your project file to keep your existing configuration.

## Getting started
1. **Reference the attributes**  
   Import `InpcFieldGenerator.Abstractions` in your project or share the project reference when developing locally.
2. **Annotate your view models**
   ```csharp
   using InpcFieldGenerator.Abstractions;
   using ReactiveUI;

   [ReactiveViewModel(NotifyOnChanging = true)]
   public partial class PersonViewModel : ReactiveObject
   {
       [ReactiveField(AlsoNotify = [nameof(FullName)])]
       public partial string FirstName { get; set; }

       [ReactiveField(AlsoNotify = [nameof(FullName)])]
       public partial string LastName { get; set; }

       public string FullName => $"{FirstName} {LastName}".Trim();
   }
   ```
3. **Build your project**  
   The generator emits partial implementations that call into your `RaisePropertyChanged` (and optionally `RaisePropertyChanging`) helpers.

> **Prerequisites:** .NET SDK 10 preview (pinned via `global.json`) and a compiler that understands the C# 14 preview `field` keyword.

## Avalonia sample application
Explore the end-to-end workflow with the desktop sample:

```bash
dotnet run --project samples/ReactiveUiSample
```

The sample uses Avalonia + ReactiveUI to show live bindings, cross-property notifications, and property-changing diagnostics powered by the generator.

## Documentation
- Attribute reference: [`docs/AttributeReference.md`](docs/AttributeReference.md)
- Implementation plan: [`docs/ImplementationPlan.md`](docs/ImplementationPlan.md)
- Contributing guide: [`docs/Contributing.md`](docs/Contributing.md)
- Changelog: [`docs/Changelog.md`](docs/Changelog.md)

## CI & releases
- Continuous integration: [`.github/workflows/ci.yml`](.github/workflows/ci.yml) builds, tests, packs, and publishes coverage on every push and PR (Ubuntu + Windows matrix).
- Release automation: [`.github/workflows/release.yml`](.github/workflows/release.yml) runs the full validation suite, pushes packages to NuGet when `NUGET_API_KEY` is available, and attaches artifacts to GitHub releases triggered by tags (`v*`) or manual dispatch.
- Packages are versioned via the shared `Directory.Build.props` settings (`VersionPrefix`/`VersionSuffix`).

## Contributing
We welcome issues and pull requests. Please read the contributing guide before opening a PR.

## License
This project is licensed under the [MIT License](LICENSE).
