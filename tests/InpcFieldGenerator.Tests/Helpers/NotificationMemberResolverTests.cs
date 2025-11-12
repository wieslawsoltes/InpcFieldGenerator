using FluentAssertions;
using InpcFieldGenerator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace InpcFieldGenerator.Tests.Helpers;

public static class NotificationMemberResolverTests
{
    [Fact]
    public static void FindsMember_On_Declaring_Type()
    {
        const string source = """
            namespace SampleApp;

            public abstract class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
            }
            """;

        var typeSymbol = GetTypeSymbol(source, "SampleApp.ViewModelBase");

        var method = NotificationMemberResolver.FindNotificationMember(typeSymbol, "RaisePropertyChanged");

        method.Should().NotBeNull();
        method!.Parameters.Should().ContainSingle();
    }

    [Fact]
    public static void Walks_Base_Types()
    {
        const string source = """
            namespace SampleApp;

            public abstract class ViewModelBase
            {
                protected void RaisePropertyChanged(string propertyName) { }
            }

            public sealed class DerivedViewModel : ViewModelBase
            {
            }
            """;

        var typeSymbol = GetTypeSymbol(source, "SampleApp.DerivedViewModel");

        var method = NotificationMemberResolver.FindNotificationMember(typeSymbol, "RaisePropertyChanged");

        method.Should().NotBeNull();
        method!.ContainingType.Name.Should().Be("ViewModelBase");
    }

    [Fact]
    public static void Rejects_Invalid_Signature()
    {
        const string source = """
            namespace SampleApp;

            public abstract class ViewModelBase
            {
                protected void RaisePropertyChanged() { }
            }
            """;

        var typeSymbol = GetTypeSymbol(source, "SampleApp.ViewModelBase");

        NotificationMemberResolver.FindNotificationMember(typeSymbol, "RaisePropertyChanged")
            .Should().BeNull();
    }

    private static INamedTypeSymbol GetTypeSymbol(string source, string metadataName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create(
            "ResolverTests",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetTypeByMetadataName(metadataName)!;
    }
}
