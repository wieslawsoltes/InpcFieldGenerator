using System.Linq;
using FluentAssertions;
using InpcFieldGenerator.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace InpcFieldGenerator.Tests.Generation;

public static class EmitterUtilitiesTests
{
    [Fact]
    public static void FormatTypeParameterList_And_Constraints_Handle_All_Cases()
    {
        const string source = """
            namespace SampleApp;

            public class GenericType<T, U, V, W, X>
                where T : class, new()
                where U : struct
                where V : unmanaged
                where W : SampleApp.BaseClass, System.IDisposable
                where X : notnull
            {
            }

            public abstract class BaseClass { }
            """;

        var compilation = CreateCompilation(source);
        var symbol = (INamedTypeSymbol)compilation.GetTypeByMetadataName("SampleApp.GenericType`5")!;

        EmitterUtilities.FormatTypeParameterList(symbol).Should().Be("<T, U, V, W, X>");

        var constraints = EmitterUtilities.FormatTypeConstraints(symbol);
        constraints.Should().Contain("where T : class, new()");
        constraints.Should().Contain("where U : struct");
        constraints.Should().Contain("where V : unmanaged");
        constraints.Should().Contain("where W : global::SampleApp.BaseClass, global::System.IDisposable");
        constraints.Should().Contain("where X : notnull");
    }

    [Fact]
    public static void FormatAccessibility_Maps_All_Values()
    {
        EmitterUtilities.FormatAccessibility(Accessibility.Public).Should().Be("public");
        EmitterUtilities.FormatAccessibility(Accessibility.Internal).Should().Be("internal");
        EmitterUtilities.FormatAccessibility(Accessibility.Protected).Should().Be("protected");
        EmitterUtilities.FormatAccessibility(Accessibility.Private).Should().Be("private");
        EmitterUtilities.FormatAccessibility(Accessibility.ProtectedOrInternal).Should().Be("protected internal");
        EmitterUtilities.FormatAccessibility(Accessibility.ProtectedAndInternal).Should().Be("private protected");
        EmitterUtilities.FormatAccessibility((Accessibility)int.MaxValue).Should().Be("internal");
    }

    [Fact]
    public static void FormatTypeKeyword_Handles_Records_And_Structs()
    {
        const string source = """
            namespace SampleApp;

            public record PersonRecord(string Name);
            public record struct Point(int X, int Y);
            public struct ValueContainer { }
            public interface IService { }
            public enum Status { None }
            """;

        var compilation = CreateCompilation(source);

        var record = (INamedTypeSymbol)compilation.GetTypeByMetadataName("SampleApp.PersonRecord")!;
        EmitterUtilities.FormatTypeKeyword(record).Should().Be("record");

        var recordStruct = (INamedTypeSymbol)compilation.GetTypeByMetadataName("SampleApp.Point")!;
        EmitterUtilities.FormatTypeKeyword(recordStruct).Should().Be("record struct");

        var structSymbol = (INamedTypeSymbol)compilation.GetTypeByMetadataName("SampleApp.ValueContainer")!;
        EmitterUtilities.FormatTypeKeyword(structSymbol).Should().Be("struct");

        var interfaceSymbol = (INamedTypeSymbol)compilation.GetTypeByMetadataName("SampleApp.IService")!;
        EmitterUtilities.FormatTypeKeyword(interfaceSymbol).Should().Be("interface");

        var enumSymbol = (INamedTypeSymbol)compilation.GetTypeByMetadataName("SampleApp.Status")!;
        EmitterUtilities.FormatTypeKeyword(enumSymbol).Should().Be("enum");
    }

    [Fact]
    public static void CreateStringLiteral_Escapes_ControlCharacters()
    {
        var literal = EmitterUtilities.CreateStringLiteral("Line1\r\nLine2\t\\\"\0\b\f");

        literal.Should().Be("\"Line1\\r\\nLine2\\t\\\\\\\"\\0\\b\\f\"");
    }

    [Fact]
    public static void FormatNamespace_Strips_GlobalPrefix()
    {
        const string source = """
            namespace SampleApp.Utilities;

            public class Utility { }
            """;

        var compilation = CreateCompilation(source);
        var symbol = compilation.GetTypeByMetadataName("SampleApp.Utilities.Utility")!;

        EmitterUtilities.FormatNamespace(symbol.ContainingNamespace).Should().Be("SampleApp.Utilities");
    }

    private static Compilation CreateCompilation(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        return CSharpCompilation.Create(
            "EmitterUtilityTests",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
