using System.Linq;
using FluentAssertions;
using InpcFieldGenerator.Abstractions;
using InpcFieldGenerator.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace InpcFieldGenerator.Tests.Configuration;

public static class ReactiveDefaultsTests
{
    [Fact]
    public static void From_Merges_Class_And_Assembly_Attributes()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            [assembly: ReactiveFieldOption(ViewModelMember = "AssemblyChanged", NotifyOnChanging = true, GenerateEqualityCheck = false, AlsoNotify = ["AssemblyProperty", "Shared"])]

            namespace SampleApp;

            [ReactiveViewModel(ViewModelMember = "ClassChanged", NotifyOnChanging = false, GenerateEqualityCheck = true)]
            public partial class PersonViewModel
            {
                public string AssemblyProperty { get; set; } = "";
                public string Shared { get; set; } = "";
                public string Local { get; set; } = "";
            }
            """;

        var compilation = CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("SampleApp.PersonViewModel");
        typeSymbol.Should().NotBeNull();

        var typeAttributes = typeSymbol!.GetAttributes();
        typeAttributes.Should().NotBeEmpty();
        typeAttributes.Select(attribute => attribute.AttributeClass?.ToDisplayString())
            .Should().Contain("InpcFieldGenerator.Abstractions.ReactiveViewModelAttribute");

        var viewModelAttribute = typeAttributes.Single(attribute => attribute.AttributeClass?.ToDisplayString() == "InpcFieldGenerator.Abstractions.ReactiveViewModelAttribute");
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        var defaults = ReactiveDefaults.From(typeAttributes.AddRange(compilation.Assembly.GetAttributes()), semanticModel, default);

        defaults.ViewModelMember.Should().Be("ClassChanged");
        defaults.NotifyOnChanging.Should().BeFalse();
        defaults.GenerateEqualityCheck.Should().BeTrue();
        defaults.AlsoNotify.Should().ContainInOrder("AssemblyProperty", "Shared");
    }

    [Fact]
    public static void From_Deduplicates_AlsoNotify_Entries()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            [assembly: ReactiveFieldOption(AlsoNotify = ["Shared", "AssemblyProperty"])]
            [assembly: ReactiveFieldOption(AlsoNotify = new[] { "AssemblyProperty", "Extra" })]

            namespace SampleApp;

            [ReactiveViewModel]
            public partial class OrderViewModel
            {
                public string Shared { get; set; } = "";
                public string AssemblyProperty { get; set; } = "";
                public string Extra { get; set; } = "";
            }
            """;

        var compilation = CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("SampleApp.OrderViewModel");
        typeSymbol.Should().NotBeNull();

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        var defaults = ReactiveDefaults.From(typeSymbol!.GetAttributes().AddRange(compilation.Assembly.GetAttributes()), semanticModel, default);

        defaults.AlsoNotify.Should().BeEquivalentTo(new[] { "Shared", "AssemblyProperty", "Extra" }, options => options.WithStrictOrdering());
    }

    [Fact]
    public static void From_Handles_Collection_Spread_In_Assembly_Options()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            [assembly: ReactiveFieldOption(AlsoNotify = ["Direct", ..new[] { "SpreadOne", "SpreadTwo" }])]

            namespace SampleApp;

            [ReactiveViewModel]
            public partial class OrderViewModel
            {
                public string Direct { get; set; } = "";
                public string SpreadOne { get; set; } = "";
                public string SpreadTwo { get; set; } = "";
            }
            """;

        var compilation = CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("SampleApp.OrderViewModel");
        typeSymbol.Should().NotBeNull();

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        var defaults = ReactiveDefaults.From(typeSymbol!.GetAttributes().AddRange(compilation.Assembly.GetAttributes()), semanticModel, default);

        defaults.AlsoNotify.Should().ContainInOrder("Direct", "SpreadOne", "SpreadTwo");
    }

    [Fact]
    public static void From_Ignores_NonArray_AlsoNotify()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            [assembly: ReactiveFieldOption(AlsoNotify = null)]

            namespace SampleApp;

            [ReactiveViewModel]
            public partial class Sample
            {
            }
            """;

        var compilation = CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("SampleApp.Sample");
        typeSymbol.Should().NotBeNull();

        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        var defaults = ReactiveDefaults.From(typeSymbol!.GetAttributes(), semanticModel, default);

        defaults.AlsoNotify.Should().BeEmpty();
    }

    private static Compilation CreateCompilation(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ReactiveFieldAttribute).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "ConfigTests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
