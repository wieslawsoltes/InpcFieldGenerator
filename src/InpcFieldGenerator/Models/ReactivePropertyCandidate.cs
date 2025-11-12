using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using InpcFieldGenerator.Configuration;
using InpcFieldGenerator.Constants;
using InpcFieldGenerator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace InpcFieldGenerator.Models;

internal sealed class ReactivePropertyCandidate
{
    private ReactivePropertyCandidate(
        IPropertySymbol propertySymbol,
        PropertyDeclarationSyntax propertySyntax,
        AttributeSyntax attributeSyntax,
        ReactiveFieldAttributeSettings propertySettings,
        ReactiveDefaults defaults)
    {
        PropertySymbol = propertySymbol;
        PropertySyntax = propertySyntax;
        AttributeSyntax = attributeSyntax;
        PropertySettings = propertySettings;
        Defaults = defaults;
    }

    public IPropertySymbol PropertySymbol { get; }

    public PropertyDeclarationSyntax PropertySyntax { get; }

    public AttributeSyntax AttributeSyntax { get; }

    public ReactiveFieldAttributeSettings PropertySettings { get; }

    public ReactiveDefaults Defaults { get; }

    public bool HasPartialKeyword => PropertySyntax.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));

    public bool HasSetter => PropertySymbol.SetMethod is not null;

    public Diagnostic CreatePartialDiagnostic()
    {
        return Diagnostic.Create(
            DiagnosticDescriptors.PropertyMustBePartial,
            PropertySyntax.Identifier.GetLocation(),
            PropertySymbol.Name);
    }

    public Diagnostic CreateSetterDiagnostic()
    {
        return Diagnostic.Create(
            DiagnosticDescriptors.PropertyMustHaveSetter,
            PropertySyntax.Identifier.GetLocation(),
            PropertySymbol.Name);
    }

    public static ReactivePropertyCandidate? From(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
        var propertySymbol = (IPropertySymbol)context.SemanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken)!;

        var attributeData = propertySymbol.GetAttributes()
            .FirstOrDefault(static attribute => attribute.AttributeClass?.ToDisplayString() == AttributeMetadataNames.ReactiveFieldAttribute);

        if (attributeData is null)
        {
            return null;
        }

        var attributeSyntax = (AttributeSyntax)attributeData.ApplicationSyntaxReference!.GetSyntax(cancellationToken);

        var propertySettings = ReactiveFieldAttributeSettings.Create(
            attributeData,
            context.SemanticModel,
            attributeSyntax,
            cancellationToken);

        var defaults = ComputeDefaults(propertySymbol, context.SemanticModel, cancellationToken);

        return new ReactivePropertyCandidate(propertySymbol, propertyDeclaration, attributeSyntax, propertySettings, defaults);
    }

    private static ReactiveDefaults ComputeDefaults(IPropertySymbol propertySymbol, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var containingType = propertySymbol.ContainingType!;
        var attributeBuilder = ImmutableArray.CreateBuilder<AttributeData>();
        attributeBuilder.AddRange(containingType.GetAttributes());

        if (containingType.ContainingAssembly is { } assemblySymbol)
        {
            attributeBuilder.AddRange(assemblySymbol.GetAttributes());
        }

        var applicableAttributes = attributeBuilder
            .Where(static attribute =>
                attribute.AttributeClass?.ToDisplayString() is AttributeMetadataNames.ReactiveViewModelAttribute or AttributeMetadataNames.ReactiveFieldOptionAttribute)
            .ToImmutableArray();

        return ReactiveDefaults.From(applicableAttributes, semanticModel, cancellationToken);
    }
}
