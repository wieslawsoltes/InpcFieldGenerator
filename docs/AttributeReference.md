# Attribute Reference

The INPC field generator is configured through three attributes shipped in `InpcFieldGenerator.Abstractions`. Apply them at the property, class, or assembly level to fine-tune how notifications are produced.

## `[ReactiveField]`
Targets `partial` property declarations. When present, the generator creates a backing field, setter logic using the C# 14 `field` keyword, and calls into your configured notification members.

| Named argument | Type | Default | Description |
| -------------- | ---- | ------- | ----------- |
| `PropertyName` | `string?` | Property identifier | Overrides the emitted property name when the semantic model needs a different casing or alias. |
| `ViewModelMember` | `string?` | `RaisePropertyChanged` | Overrides the notification method used after assignment. Must resolve to an instance method with signature `void Member(string propertyName)`. |
| `NotifyOnChanging` | `bool` | `false` | Emits a call to `RaisePropertyChanging` (or the configured member) before `field = value`. |
| `AlsoNotify` | `string[]` | `Array.Empty<string>()` | Additional properties to raise after the main notification. Useful for computed properties (e.g., `FullName`). |
| `GenerateEqualityCheck` | `bool` | `true` | Emits an equality guard using `EqualityComparer<T>.Default`. Disable when you always want the setter body to run (e.g., timestamp updates). |

> **Remember:** the containing type must provide the members referenced in `ViewModelMember` and `NotifyOnChanging`, typically helpers on your ReactiveUI base class.

## `[ReactiveViewModel]`
Apply to partial classes to configure defaults for all `[ReactiveField]` members inside the class. Property-level attributes can still override these values.

| Named argument | Type | Default | Description |
| -------------- | ---- | ------- | ----------- |
| `ViewModelMember` | `string?` | `RaisePropertyChanged` | Default post-set notification method. |
| `NotifyOnChanging` | `bool` | `false` | Enables `RaisePropertyChanging` calls by default. |
| `GenerateEqualityCheck` | `bool` | `true` | Controls whether equality guards are generated. |

## `[ReactiveFieldOption]`
Define assembly-wide defaults (e.g., in `AssemblyInfo.cs`) to keep behavior consistent across multiple view models.

| Named argument | Type | Default | Description |
| -------------- | ---- | ------- | ----------- |
| `ViewModelMember` | `string?` | `RaisePropertyChanged` | Assembly-wide default notification member. |
| `NotifyOnChanging` | `bool` | `false` | Assembly-wide default for changing notifications. |
| `GenerateEqualityCheck` | `bool` | `true` | Assembly-wide default for equality guards. |
| `AlsoNotify` | `string[]` | `Array.Empty<string>()` | Properties added to every `[ReactiveField]` in the assembly unless overridden. |

## Example: consolidating defaults
```csharp
[assembly: ReactiveFieldOption(NotifyOnChanging = true)]

[ReactiveViewModel(ViewModelMember = nameof(RaiseMyPropertyChanged))]
public partial class DocumentViewModel : ReactiveViewModelBase
{
    [ReactiveField(AlsoNotify = [nameof(FullName)])]
    public partial string Title { get; set; }

    [ReactiveField]
    public partial string Author { get; set; }

    public string FullName => $"{Author}: {Title}";

    protected void RaiseMyPropertyChanged(string propertyName)
    {
        RaisePropertyChanged(propertyName); // Delegate to helper
    }
}
```

## MSBuild integration
- The analyzer package ships a `buildTransitive` props file that sets `LangVersion=preview` if your project does not already specify one. Opt out by setting `<InpcFieldGeneratorSetLangVersion>false</InpcFieldGeneratorSetLangVersion>` in the consuming project.
- Install both `InpcFieldGenerator` and `InpcFieldGenerator.Abstractions` packages to ensure the generator and attribute definitions stay in sync.
