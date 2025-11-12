using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using InpcFieldGenerator.Configuration;
using InpcFieldGenerator.Diagnostics;
using InpcFieldGenerator.Generation;
using InpcFieldGenerator.Helpers;
using InpcFieldGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace InpcFieldGenerator.Incremental;

/// <summary>
/// Incremental generator entry point for the INPC field generator.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class InpcReactiveFieldGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Configures the incremental generator pipeline.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            const string hintName = "ReactiveFieldGenerator.Attributes.g.cs";
            const string source = "// Attributes distributed via reference assembly.\n";
            ctx.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        });

        var reactiveProperties = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is PropertyDeclarationSyntax { AttributeLists.Count: > 0 },
                static (syntaxContext, cancellationToken) => ReactivePropertyCandidate.From(syntaxContext, cancellationToken))
            .Where(static candidate => candidate is not null)
            .Select(static (candidate, _) => candidate!);

        var groupedCandidates = reactiveProperties.Collect();

        context.RegisterSourceOutput(groupedCandidates, static (productionContext, candidates) =>
        {
            if (candidates.IsDefaultOrEmpty)
            {
                return;
            }

            var generationGroups = new Dictionary<INamedTypeSymbol, List<PropertyGenerationModel>>(SymbolEqualityComparer.Default);

            foreach (var candidate in candidates)
            {
                if (!candidate.HasPartialKeyword)
                {
                    productionContext.ReportDiagnostic(candidate.CreatePartialDiagnostic());
                    continue;
                }

                if (!candidate.HasSetter)
                {
                    productionContext.ReportDiagnostic(candidate.CreateSetterDiagnostic());
                    continue;
                }

                var attributeSettings = candidate.PropertySettings;
                var defaults = candidate.Defaults;
                var configuration = ReactivePropertyConfiguration.Create(candidate.PropertySymbol, attributeSettings, defaults);
                var containingType = candidate.PropertySymbol.ContainingType!;

                var changedMethod = NotificationMemberResolver.FindNotificationMember(containingType, configuration.ChangedNotificationMember);

                if (changedMethod is null)
                {
                    productionContext.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.NotificationMemberMissing,
                        candidate.PropertySyntax.Identifier.GetLocation(),
                        containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        configuration.ChangedNotificationMember));
                    continue;
                }

                string? changingMemberName = null;

                if (configuration.NotifyOnChanging)
                {
                    const string propertyChangingMember = "RaisePropertyChanging";
                    var changingMethod = NotificationMemberResolver.FindNotificationMember(containingType, propertyChangingMember);

                    if (changingMethod is null)
                    {
                        productionContext.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ChangingNotificationMemberMissing,
                            candidate.PropertySyntax.Identifier.GetLocation(),
                            containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                            propertyChangingMember));
                        continue;
                    }

                    changingMemberName = propertyChangingMember;
                }

                if (!configuration.AdditionalNotifications.IsDefaultOrEmpty)
                {
                    var attributeLocation = GetAttributeLocation(candidate.AttributeSyntax, candidate.PropertySyntax.Identifier.GetLocation());

                    foreach (var notification in configuration.AdditionalNotifications)
                    {
                        if (!PropertyExists(containingType, notification))
                        {
                            productionContext.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.AlsoNotifyPropertyMissing,
                                attributeLocation,
                                candidate.PropertySymbol.Name,
                                notification,
                                containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                            goto NextCandidate;
                        }
                    }
                }

                AddGenerationModel(
                    generationGroups,
                    containingType,
                    new PropertyGenerationModel(
                        candidate.PropertySymbol,
                        configuration.PropertyName,
                        Generation.EmitterUtilities.FormatType(candidate.PropertySymbol.Type),
                        configuration.ChangedNotificationMember,
                        changingMemberName,
                        configuration.NotifyOnChanging,
                        configuration.GenerateEqualityCheck,
                        configuration.AdditionalNotifications));

            NextCandidate:
                ;
            }

            foreach (var group in generationGroups)
            {
                var propertyModels = group.Value
                    .OrderBy(model => model.PropertyName, StringComparer.Ordinal)
                    .ToImmutableArray();

                var source = PropertySourceEmitter.Emit(group.Key, propertyModels);
                var hintName = CreateHintName(group.Key);
                productionContext.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
            }
        });
    }

    private static void AddGenerationModel(
        IDictionary<INamedTypeSymbol, List<PropertyGenerationModel>> generationGroups,
        INamedTypeSymbol containingType,
        PropertyGenerationModel model)
    {
        if (!generationGroups.TryGetValue(containingType, out var list))
        {
            list = new List<PropertyGenerationModel>();
            generationGroups[containingType] = list;
        }

        list.Add(model);
    }

    private static bool PropertyExists(INamedTypeSymbol containingType, string propertyName)
    {
        for (var current = containingType; current is not null; current = current.BaseType)
        {
            if (current.GetMembers(propertyName).OfType<IPropertySymbol>().Any())
            {
                return true;
            }
        }

        return false;
    }

    private static Location GetAttributeLocation(AttributeSyntax attributeSyntax, Location fallback)
    {
        return attributeSyntax?.GetLocation() ?? fallback;
    }

    private static string CreateHintName(INamedTypeSymbol typeSymbol)
    {
        var fullyQualified = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sansGlobal = fullyQualified.StartsWith("global::", StringComparison.Ordinal)
            ? fullyQualified.Substring("global::".Length)
            : fullyQualified;
        var builder = new StringBuilder(sansGlobal.Length + 16);

        foreach (var character in sansGlobal)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        builder.Append("_ReactiveField.g.cs");
        return builder.ToString();
    }
}
