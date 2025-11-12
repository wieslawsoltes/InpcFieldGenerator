using System.Collections.Immutable;
using FluentAssertions;
using InpcFieldGenerator.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace InpcFieldGenerator.Tests.Generation;

public static class PropertyGenerationModelTests
{
    [Fact]
    public static void Stores_Constructor_Values()
    {
        const string source = """
            namespace SampleApp;

            public partial class Container
            {
                public int Value { get; set; }
            }
            """;

        var compilation = CSharpCompilation.Create(
            "PropertyModelTest",
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var propertySymbol = (IPropertySymbol)compilation.GetTypeByMetadataName("SampleApp.Container")!
            .GetMembers("Value")
            .Single();

        var model = new PropertyGenerationModel(
            propertySymbol,
            "Value",
            "int",
            "RaisePropertyChanged",
            "RaisePropertyChanging",
            true,
            false,
            ImmutableArray.Create("Other"));

        model.PropertySymbol.Should().Be(propertySymbol);
        model.PropertyName.Should().Be("Value");
        model.NotifyOnChanging.Should().BeTrue();
        model.GenerateEqualityCheck.Should().BeFalse();
        model.AdditionalNotifications.Should().ContainSingle().Which.Should().Be("Other");
    }
}
