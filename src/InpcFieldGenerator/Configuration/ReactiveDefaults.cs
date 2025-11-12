using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using InpcFieldGenerator.Abstractions;
using InpcFieldGenerator.Constants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace InpcFieldGenerator.Configuration;

[ExcludeFromCodeCoverage]
internal sealed record ReactiveDefaults(
    string? ViewModelMember,
    bool? NotifyOnChanging,
    bool? GenerateEqualityCheck,
    ImmutableArray<string> AlsoNotify)
{
    public static ReactiveDefaults From(ImmutableArray<AttributeData> attributes, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        string? viewModelMember = null;
        bool? notifyOnChanging = null;
        bool? generateEqualityCheck = null;
        var alsoNotify = ImmutableArray.CreateBuilder<string>();
        var alsoNotifySet = new HashSet<string>(System.StringComparer.Ordinal);

        foreach (var attribute in attributes)
        {
            var displayName = attribute.AttributeClass?.ToDisplayString();

            switch (displayName)
            {
                case AttributeMetadataNames.ReactiveViewModelAttribute:
                    ExtractViewModelAttribute(attribute, semanticModel, cancellationToken, ref viewModelMember, ref notifyOnChanging, ref generateEqualityCheck);
                    break;
                case AttributeMetadataNames.ReactiveFieldOptionAttribute:
                    ExtractOptionAttribute(attribute, semanticModel, cancellationToken, ref viewModelMember, ref notifyOnChanging, ref generateEqualityCheck, alsoNotify, alsoNotifySet);
                    break;
            }
        }

        return new ReactiveDefaults(
            viewModelMember,
            notifyOnChanging,
            generateEqualityCheck,
            alsoNotify.ToImmutable());
    }

    private static void ExtractViewModelAttribute(
        AttributeData attribute,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        ref string? viewModelMember,
        ref bool? notifyOnChanging,
        ref bool? generateEqualityCheck)
    {
        if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax attributeSyntax ||
            attributeSyntax.ArgumentList is null)
        {
            return;
        }

        foreach (var argument in attributeSyntax.ArgumentList.Arguments)
        {
            var name = argument.NameEquals?.Name.Identifier.ValueText;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            switch (name)
            {
                case nameof(ReactiveViewModelAttribute.ViewModelMember):
                    if (TryEvaluateString(semanticModel, argument.Expression, cancellationToken, out var member))
                    {
                        viewModelMember ??= member;
                    }

                    break;
                case nameof(ReactiveViewModelAttribute.NotifyOnChanging):
                    if (TryEvaluateBoolean(semanticModel, argument.Expression, cancellationToken, out var notify))
                    {
                        notifyOnChanging ??= notify;
                    }

                    break;
                case nameof(ReactiveViewModelAttribute.GenerateEqualityCheck):
                    if (TryEvaluateBoolean(semanticModel, argument.Expression, cancellationToken, out var equality))
                    {
                        generateEqualityCheck ??= equality;
                    }

                    break;
            }
        }
    }

    private static void ExtractOptionAttribute(
        AttributeData attribute,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        ref string? viewModelMember,
        ref bool? notifyOnChanging,
        ref bool? generateEqualityCheck,
        ImmutableArray<string>.Builder alsoNotify,
        ISet<string> alsoNotifySet)
    {
        if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax attributeSyntax ||
            attributeSyntax.ArgumentList is null)
        {
            return;
        }

        foreach (var argument in attributeSyntax.ArgumentList.Arguments)
        {
            var name = argument.NameEquals?.Name.Identifier.ValueText;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            switch (name)
            {
                case nameof(ReactiveFieldOptionAttribute.ViewModelMember):
                    if (TryEvaluateString(semanticModel, argument.Expression, cancellationToken, out var member))
                    {
                        viewModelMember ??= member;
                    }

                    break;
                case nameof(ReactiveFieldOptionAttribute.NotifyOnChanging):
                    if (TryEvaluateBoolean(semanticModel, argument.Expression, cancellationToken, out var notify))
                    {
                        notifyOnChanging ??= notify;
                    }

                    break;
                case nameof(ReactiveFieldOptionAttribute.GenerateEqualityCheck):
                    if (TryEvaluateBoolean(semanticModel, argument.Expression, cancellationToken, out var equality))
                    {
                        generateEqualityCheck ??= equality;
                    }

                    break;
                case nameof(ReactiveFieldOptionAttribute.AlsoNotify):
                    foreach (var entry in EvaluateStringArrayValues(semanticModel, argument.Expression, cancellationToken))
                    {
                        if (alsoNotifySet.Add(entry))
                        {
                            alsoNotify.Add(entry);
                        }
                    }

                    break;
            }
        }
    }

    private static bool TryEvaluateBoolean(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken, out bool result)
    {
        var constantValue = semanticModel.GetConstantValue(expression, cancellationToken);
        if (constantValue.HasValue && constantValue.Value is bool boolValue)
        {
            result = boolValue;
            return true;
        }

        result = default;
        return false;
    }

    private static bool TryEvaluateString(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken, out string? result)
    {
        var constantValue = semanticModel.GetConstantValue(expression, cancellationToken);
        if (constantValue.HasValue && constantValue.Value is string stringValue)
        {
            result = stringValue;
            return true;
        }

        result = null;
        return false;
    }

    private static IEnumerable<string> EvaluateStringArrayValues(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        if (expression is CollectionExpressionSyntax collectionExpression)
        {
            foreach (var value in EvaluateCollectionExpression(semanticModel, collectionExpression, cancellationToken))
            {
                yield return value;
            }

            yield break;
        }

        InitializerExpressionSyntax? initializer = expression switch
        {
            ArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer,
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer,
            InitializerExpressionSyntax inlineInitializer => inlineInitializer,
            _ => null,
        };

        if (initializer is null)
        {
            yield break;
        }

        foreach (var item in initializer.Expressions)
        {
            if (TryEvaluateString(semanticModel, item, cancellationToken, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                yield return value!;
            }
        }
    }

    private static IEnumerable<string> EvaluateCollectionExpression(
        SemanticModel semanticModel,
        CollectionExpressionSyntax collectionExpression,
        CancellationToken cancellationToken)
    {
        foreach (var element in collectionExpression.Elements)
        {
            switch (element)
            {
                case ExpressionElementSyntax expressionElement:
                    if (TryEvaluateString(semanticModel, expressionElement.Expression, cancellationToken, out var value) &&
                        !string.IsNullOrWhiteSpace(value))
                    {
                        yield return value!;
                    }

                    break;
                case SpreadElementSyntax spreadElement:
                    foreach (var spreadValue in EvaluateStringArrayValues(semanticModel, spreadElement.Expression, cancellationToken))
                    {
                        yield return spreadValue;
                    }

                    break;
            }
        }
    }
}
