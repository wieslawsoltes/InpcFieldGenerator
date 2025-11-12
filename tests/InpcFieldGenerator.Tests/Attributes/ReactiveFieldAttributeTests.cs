using System.Linq;
using FluentAssertions;
using InpcFieldGenerator.Abstractions;
using InpcFieldGenerator.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using System.Threading;

namespace InpcFieldGenerator.Tests.Attributes;

public static class ReactiveFieldAttributeTests
{
    [Fact]
    public static void Constructor_Sets_Defaults()
    {
        var attribute = new ReactiveFieldAttribute();

        attribute.NotifyOnChanging.Should().BeFalse();
        attribute.GenerateEqualityCheck.Should().BeTrue();
        attribute.ViewModelMember.Should().BeNull();
        attribute.AlsoNotify.Should().BeEmpty();
    }

    [Fact]
    public static void AlsoNotify_Rejects_Null()
    {
        var attribute = new ReactiveFieldAttribute
        {
            AlsoNotify = null!,
        };

        attribute.AlsoNotify.Should().BeEmpty();
    }

    [Fact]
    public static void AlsoNotify_Preserves_Array_When_NotEmpty()
    {
        var values = new[] { "FullName" };
        var attribute = new ReactiveFieldAttribute
        {
            AlsoNotify = values,
        };

        attribute.AlsoNotify.Should().BeSameAs(values);
    }

    [Fact]
    public static void AttributeData_Extracts_Named_Arguments()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public class PersonViewModel
            {
                [ReactiveField(PropertyName = "GivenName", NotifyOnChanging = true, GenerateEqualityCheck = false, AlsoNotify = ["FullName", "FullName"], ViewModelMember = "NotifyChanged")]
                public string FirstName { get; set; }

                public string FullName { get; set; }
            }
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ReactiveFieldAttribute).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "AttributeExtraction",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var propertyDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .First(property => property.AttributeLists.Count > 0);

        var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration) as IPropertySymbol;

        propertySymbol.Should().NotBeNull();
        var attributeData = propertySymbol!.GetAttributes().First();
        var attributeSyntax = propertyDeclaration.AttributeLists
            .SelectMany(static list => list.Attributes)
            .Single();

        var settings = ReactiveFieldAttributeSettings.Create(
            attributeData,
            semanticModel,
            attributeSyntax,
            CancellationToken.None);

        settings.PropertyName.Should().Be("GivenName");
        settings.NotifyOnChanging.Should().BeTrue();
        settings.GenerateEqualityCheck.Should().BeFalse();
        settings.ViewModelMember.Should().Be("NotifyChanged");
        settings.AlsoNotify.Should().ContainSingle().Which.Should().Be("FullName");
    }

    [Fact]
    public static void AttributeData_Handles_Spread_Collection_Expression()
    {
        const string source = """
            using InpcFieldGenerator.Abstractions;

            namespace SampleApp;

            public class PersonViewModel
            {
                [ReactiveField(AlsoNotify = ["FullName", ..new[] { "Alias" }])]
                public string FirstName { get; set; }

                public string FullName { get; set; }
                public string Alias { get; set; }
            }
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ReactiveFieldAttribute).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "AttributeSpreadExtraction",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var propertyDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .First(property => property.AttributeLists.Count > 0);

        var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration) as IPropertySymbol;

        propertySymbol.Should().NotBeNull();
        var attributeData = propertySymbol!.GetAttributes().First();
        var attributeSyntax = propertyDeclaration.AttributeLists
            .SelectMany(static list => list.Attributes)
            .Single();

        var settings = ReactiveFieldAttributeSettings.Create(
            attributeData,
            semanticModel,
            attributeSyntax,
            CancellationToken.None);

        settings.AlsoNotify.Should().BeEquivalentTo(new[] { "FullName", "Alias" }, options => options.WithStrictOrdering());
    }

    [Fact]
    public static void PropertyName_Property_SetRoundTrips()
    {
        var attribute = new ReactiveFieldAttribute
        {
            PropertyName = "Custom",
        };

        attribute.PropertyName.Should().Be("Custom");
    }
}
