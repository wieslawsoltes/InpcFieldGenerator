using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using FluentAssertions;
using InpcFieldGenerator.Abstractions;
using InpcFieldGenerator.Incremental;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyTests;
using VerifyXunit;
using System.Threading.Tasks;

namespace InpcFieldGenerator.Tests.Incremental;

public static class InpcReactiveFieldGeneratorTests
{
    [Fact]
    public static void ReportsDiagnostic_When_Property_Not_Partial()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            [ReactiveViewModel]
            public partial class PersonViewModel
            {
                [ReactiveField]
                public string FirstName { get; set; }
            }
            """;

        var (_, _, generatorDiagnostics) = RunGenerator(source);

        generatorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "INPCFG001");
    }

    [Fact]
    public static void DoesNothing_When_No_ReactiveField_Attributes()
    {
        const string source = """
            using System;

            namespace SampleApp;

            public partial class PersonViewModel
            {
                [Obsolete]
                public int Age { get; set; }
            }
            """;

        var (driver, _, diagnostics) = RunGenerator(source);

        diagnostics.Should().BeEmpty();
        var generatedSources = driver.GetRunResult().Results.SelectMany(result => result.GeneratedSources).ToArray();
        generatedSources.Should().ContainSingle();
        generatedSources[0].HintName.Should().Be("ReactiveFieldGenerator.Attributes.g.cs");
    }

    [Fact]
    public static void DoesNotReportDiagnostic_For_Partial_Property()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public abstract partial class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
            }

            [ReactiveViewModel]
            public partial class PersonViewModel : ViewModelBase
            {
                [ReactiveField]
                public partial string FirstName { get; set; }
            }
            """;

        var (_, updatedCompilation, generatorDiagnostics) = RunGenerator(source);

        generatorDiagnostics.Should().BeEmpty();
        updatedCompilation.GetDiagnostics().Should().NotContain(diagnostic => diagnostic.Id == "INPCFG001");
    }

    [Fact]
    public static void ReportsDiagnostic_When_Property_Lacks_Setter()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public abstract partial class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
            }

            [ReactiveViewModel]
            public partial class PersonViewModel : ViewModelBase
            {
                [ReactiveField]
                public partial string FirstName { get; }
            }
            """;

        var (_, _, generatorDiagnostics) = RunGenerator(source);

        generatorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "INPCFG002");
    }

    [Fact]
    public static void ReportsDiagnostic_When_RaisePropertyChanged_Missing()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            [ReactiveViewModel]
            public partial class PersonViewModel
            {
                [ReactiveField]
                public partial string FirstName { get; set; }
            }
            """;

        var (_, _, generatorDiagnostics) = RunGenerator(source);

        generatorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "INPCFG003");
    }

    [Fact]
    public static void ReportsDiagnostic_When_RaisePropertyChanging_Missing()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public abstract partial class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
            }

            [ReactiveViewModel]
            public partial class PersonViewModel : ViewModelBase
            {
                [ReactiveField(NotifyOnChanging = true)]
                public partial string FirstName { get; set; }
            }
            """;

        var (_, _, generatorDiagnostics) = RunGenerator(source);

        generatorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "INPCFG004");
    }

    [Fact]
    public static void ReportsDiagnostic_When_AlsoNotify_Target_Missing()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public abstract partial class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
            }

            [ReactiveViewModel]
            public partial class PersonViewModel : ViewModelBase
            {
                [ReactiveField(AlsoNotify = new[] { "Missing" })]
                public partial string FirstName { get; set; }
            }
            """;

        var (_, _, generatorDiagnostics) = RunGenerator(source);

        generatorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "INPCFG005");
    }

    [Fact]
    public static Task Generates_Expected_Source_With_Notifications()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public abstract partial class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
                protected void RaisePropertyChanging(string propertyName) { }
            }

            [ReactiveViewModel]
            public partial class PersonViewModel : ViewModelBase
            {
                [ReactiveField(NotifyOnChanging = true, AlsoNotify = new[] { "FullName" })]
                public partial string FirstName { get; set; }

                public string FullName => FirstName;
            }
            """;

        var (driver, _, diagnostics) = RunGenerator(source);

        diagnostics.Should().BeEmpty();

        var generatedSource = GetGeneratedSource(driver);

        return VerifyGenerated(generatedSource, "Notifications");
    }

    [Fact]
    public static Task Generates_Source_Without_Equality_Check_When_Disabled()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public abstract partial class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
            }

            [ReactiveViewModel]
            public partial class PersonViewModel : ViewModelBase
            {
                [ReactiveField(GenerateEqualityCheck = false)]
                public partial int Age { get; set; }
            }
            """;

        var (driver, _, diagnostics) = RunGenerator(source);

        diagnostics.Should().BeEmpty();

        var generatedSource = GetGeneratedSource(driver);
        return VerifyGenerated(generatedSource, "NoEqualityGuard");
    }

    [Fact]
    public static Task Generates_Source_Using_ViewModel_Defaults()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            [assembly: ReactiveFieldOption(AlsoNotify = new[] { "AssemblyProperty" })]

            namespace SampleApp;

            public abstract partial class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
                protected void RaisePropertyChanging(string propertyName) { }
                protected void OnChanged(string propertyName) { }
            }

            [ReactiveViewModel(ViewModelMember = nameof(OnChanged), NotifyOnChanging = true, GenerateEqualityCheck = false)]
            public partial class PersonViewModel : ViewModelBase
            {
                [ReactiveField]
                public partial string FirstName { get; set; }

                public string AssemblyProperty { get; set; } = "";
            }
            """;

        var (driver, _, diagnostics) = RunGenerator(source);

        diagnostics.Should().BeEmpty();

        var generatedSource = GetGeneratedSource(driver);
        return VerifyGenerated(generatedSource, "ClassDefaults");
    }

    [Fact]
    public static Task Generates_Source_For_BaseClass_AlsoNotify()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public abstract partial class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
                protected void RaisePropertyChanging(string propertyName) { }

                public string FullName { get; set; } = "";
            }

            [ReactiveViewModel]
            public partial class PersonViewModel : ViewModelBase
            {
                [ReactiveField(AlsoNotify = new[] { "FullName" }, NotifyOnChanging = true)]
                public partial string FirstName { get; set; }
            }
            """;

        var (driver, _, diagnostics) = RunGenerator(source);

        diagnostics.Should().BeEmpty();

        var generatedSource = GetGeneratedSource(driver);
        return VerifyGenerated(generatedSource, "BaseAlsoNotify");
    }

    [Fact]
    public static Task Generates_Source_With_Custom_ViewModel_Member()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public abstract partial class ViewModelBase
            {
                protected void Notify(string propertyName) { }
            }

            [ReactiveViewModel]
            public partial class PersonViewModel : ViewModelBase
            {
                [ReactiveField(ViewModelMember = nameof(Notify))]
                public partial string FirstName { get; set; }
            }
            """;

        var (driver, _, diagnostics) = RunGenerator(source);

        diagnostics.Should().BeEmpty();

        var generatedSource = GetGeneratedSource(driver);
        return VerifyGenerated(generatedSource, "CustomMember");
    }

    [Fact]
    public static Task Generates_Blank_Line_Between_Properties()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public abstract partial class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
            }

            [ReactiveViewModel]
            public partial class PersonViewModel : ViewModelBase
            {
                [ReactiveField]
                public partial string FirstName { get; set; }

                [ReactiveField]
                public partial string LastName { get; set; }
            }
            """;

        var (driver, _, diagnostics) = RunGenerator(source);

        diagnostics.Should().BeEmpty();

        var generatedSource = GetGeneratedSource(driver);
        return VerifyGenerated(generatedSource, "MultipleProperties");
    }

    private static (GeneratorDriver Driver, Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(string source)
    {
        var parseOptions = new CSharpParseOptions(languageVersion: LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8), parseOptions);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ReactiveFieldAttribute).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new InpcReactiveFieldGenerator();
        var sourceGenerator = generator.AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { sourceGenerator },
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

        return (driver, updatedCompilation, diagnostics);
    }

    private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string GetGeneratedSource(GeneratorDriver driver)
    {
        return driver
            .GetRunResult()
            .Results
            .SelectMany(result => result.GeneratedSources)
            .Single(item => item.HintName.EndsWith("_ReactiveField.g.cs", StringComparison.Ordinal))
            .SourceText
            .ToString();
    }

    private static Task VerifyGenerated(string source, string scenario)
    {
        return Verifier.Verify(new
        {
            Scenario = scenario,
            Source = NormalizeLineEndings(source),
        });
    }
}
