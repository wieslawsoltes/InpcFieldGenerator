using System;
using FluentAssertions;
using InpcFieldGenerator.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace InpcFieldGenerator.Tests.Generation;

public static class EmitterUtilitiesTests
{
    [Fact]
    public static void FormatType_Returns_FullyQualified_Name()
    {
        const string source = """
            namespace SampleApp;

            public class Simple
            {
            }
            """;

        var typeSymbol = GetTypeSymbol(source, "SampleApp.Simple");
        var result = EmitterUtilities.FormatType(typeSymbol);

        result.Should().Be("global::SampleApp.Simple");
    }

    [Fact]
    public static void FormatTypeParameterList_ReturnsExpectedValues()
    {
        const string source = """
            namespace SampleApp;

            public class Generic<T, U>
            {
            }
            """;

        var typeSymbol = GetTypeSymbol(source, "SampleApp.Generic`2");

        EmitterUtilities.FormatTypeParameterList(typeSymbol).Should().Be("<T, U>");

        const string nonGenericSource = """
            namespace SampleApp;

            public class NonGeneric
            {
            }
            """;

        var nonGenericSymbol = GetTypeSymbol(nonGenericSource, "SampleApp.NonGeneric");
        EmitterUtilities.FormatTypeParameterList(nonGenericSymbol).Should().BeEmpty();
    }

    [Fact]
    public static void FormatTypeConstraints_ReturnsEmpty_When_NoConstraints()
    {
        const string source = """
            namespace SampleApp;

            public class NoConstraints<T>
            {
            }
            """;

        var typeSymbol = GetTypeSymbol(source, "SampleApp.NoConstraints`1");
        var result = EmitterUtilities.FormatTypeConstraints(typeSymbol);

        result.Should().BeEmpty();
    }

    [Fact]
    public static void FormatTypeConstraints_ReturnsClauses_When_Constraints_Present()
    {
        const string source = """
            namespace SampleApp;

            public abstract class SampleBase { }

            public class WithConstraints<TNotNull, TReference, TUnmanaged, TValue, TNew, TDerived, TNone>
                where TNotNull : notnull
                where TReference : class
                where TUnmanaged : unmanaged
                where TValue : struct, System.IComparable<int>
                where TNew : new()
                where TDerived : SampleBase
            {
            }
            """;

        var typeSymbol = GetTypeSymbol(source, "SampleApp.WithConstraints`7");
        var result = EmitterUtilities.FormatTypeConstraints(typeSymbol);

        result.Should().Contain("where TNotNull : notnull");
        result.Should().Contain("where TReference : class");
        result.Should().Contain("where TUnmanaged : unmanaged");
        result.Should().Contain("where TValue : struct, global::System.IComparable<int>");
        result.Should().Contain("where TNew : new()");
        result.Should().Contain("where TDerived : global::SampleApp.SampleBase");
        result.Should().NotContain("TNone");
    }

    [Fact]
    public static void FormatNamespace_Trims_Global_Prefix()
    {
        const string source = """
            namespace SampleApp.Sub;

            public class Sample
            {
            }
            """;

        var typeSymbol = GetTypeSymbol(source, "SampleApp.Sub.Sample");
        var namespaceSymbol = typeSymbol.ContainingNamespace;

        var result = EmitterUtilities.FormatNamespace(namespaceSymbol);
        result.Should().Be("SampleApp.Sub");
    }

    [Fact]
    public static void FormatNamespace_Allows_Global_Namespace()
    {
        const string source = """
            public class GlobalSample
            {
            }
            """;

        var typeSymbol = GetTypeSymbol(source, "GlobalSample");
        var namespaceSymbol = typeSymbol.ContainingNamespace;

        var result = EmitterUtilities.FormatNamespace(namespaceSymbol);
        result.Should().Be("<global namespace>");
    }

    [Theory]
    [InlineData(Accessibility.Public, "public")]
    [InlineData(Accessibility.Internal, "internal")]
    [InlineData(Accessibility.Protected, "protected")]
    [InlineData(Accessibility.Private, "private")]
    [InlineData(Accessibility.ProtectedOrInternal, "protected internal")]
    [InlineData(Accessibility.ProtectedAndInternal, "private protected")]
    [InlineData((Accessibility)int.MaxValue, "internal")]
    public static void FormatAccessibility_ReturnsExpectedValues(Accessibility accessibility, string expected)
    {
        EmitterUtilities.FormatAccessibility(accessibility).Should().Be(expected);
    }

    [Fact]
    public static void FormatTypeKeyword_ReturnsExpectedValues()
    {
        const string source = """
            namespace SampleApp;

            public class RegularClass { }
            public struct SampleStruct { }
            public interface ISample { }
            public enum SampleEnum { None }
            public record SampleRecord;
            public record struct SampleRecordStruct;
            """;

        var compilation = CreateCompilation(source);

        EmitterUtilities.FormatTypeKeyword(GetTypeSymbol(compilation, "SampleApp.RegularClass"))
            .Should().Be("class");
        EmitterUtilities.FormatTypeKeyword(GetTypeSymbol(compilation, "SampleApp.SampleStruct"))
            .Should().Be("struct");
        EmitterUtilities.FormatTypeKeyword(GetTypeSymbol(compilation, "SampleApp.ISample"))
            .Should().Be("interface");
        EmitterUtilities.FormatTypeKeyword(GetTypeSymbol(compilation, "SampleApp.SampleEnum"))
            .Should().Be("enum");
        EmitterUtilities.FormatTypeKeyword(GetTypeSymbol(compilation, "SampleApp.SampleRecord"))
            .Should().Be("record");
        EmitterUtilities.FormatTypeKeyword(GetTypeSymbol(compilation, "SampleApp.SampleRecordStruct"))
            .Should().Be("record struct");
    }

    [Fact]
    public static void CreateStringLiteral_Escapes_SpecialCharacters()
    {
        const string value = "\"\\\n\r\t\0\b\f";
        var result = EmitterUtilities.CreateStringLiteral(value);

        result.Should().Be("\"\\\"\\\\\\n\\r\\t\\0\\b\\f\"");
    }

    private static INamedTypeSymbol GetTypeSymbol(string source, string metadataName)
    {
        var compilation = CreateCompilation(source);
        return GetTypeSymbol(compilation, metadataName);
    }

    private static INamedTypeSymbol GetTypeSymbol(Compilation compilation, string metadataName)
    {
        var typeSymbol = compilation.GetTypeByMetadataName(metadataName);
        typeSymbol.Should().NotBeNull($"Expected to find type '{metadataName}' in the test compilation.");
        return typeSymbol!;
    }

    private static Compilation CreateCompilation(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
        };

        return CSharpCompilation.Create(
            assemblyName: "EmitterUtilitiesTests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
