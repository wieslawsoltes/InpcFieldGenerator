using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using InpcFieldGenerator.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace InpcFieldGenerator.Configuration;

[ExcludeFromCodeCoverage]
internal sealed record ReactiveFieldAttributeSettings(
    string? PropertyName,
    string? ViewModelMember,
    bool? NotifyOnChanging,
    bool? GenerateEqualityCheck,
    ImmutableArray<string> AlsoNotify)
{
    public static ReactiveFieldAttributeSettings Create(
        AttributeData attribute,
        SemanticModel semanticModel,
        AttributeSyntax attributeSyntax,
        CancellationToken cancellationToken)
    {
        string? propertyName = null;
        string? viewModelMember = null;
        bool? notifyOnChanging = null;
        bool? generateEqualityCheck = null;
        ImmutableArray<string> alsoNotify = ImmutableArray<string>.Empty;

        if (attributeSyntax.ArgumentList is not null)
        {
            foreach (var argument in attributeSyntax.ArgumentList.Arguments)
            {
                var argumentName = argument.NameEquals?.Name.Identifier.ValueText;
                if (string.IsNullOrEmpty(argumentName))
                {
                    continue;
                }

                switch (argumentName)
                {
                    case nameof(ReactiveFieldAttribute.PropertyName):
                        if (TryEvaluateString(semanticModel, argument.Expression, cancellationToken, out var explicitPropertyName))
                        {
                            propertyName = explicitPropertyName;
                        }

                        break;
                    case nameof(ReactiveFieldAttribute.ViewModelMember):
                        if (TryEvaluateString(semanticModel, argument.Expression, cancellationToken, out var memberName))
                        {
                            viewModelMember = memberName;
                        }

                        break;
                    case nameof(ReactiveFieldAttribute.NotifyOnChanging):
                        if (TryEvaluateBoolean(semanticModel, argument.Expression, cancellationToken, out var notify))
                        {
                            notifyOnChanging = notify;
                        }

                        break;
                    case nameof(ReactiveFieldAttribute.GenerateEqualityCheck):
                        if (TryEvaluateBoolean(semanticModel, argument.Expression, cancellationToken, out var equality))
                        {
                            generateEqualityCheck = equality;
                        }

                        break;
                    case nameof(ReactiveFieldAttribute.AlsoNotify):
                        alsoNotify = EvaluateStringArray(semanticModel, argument.Expression, cancellationToken);
                        break;
                }
            }
        }

        return new ReactiveFieldAttributeSettings(
            propertyName,
            viewModelMember,
            notifyOnChanging,
            generateEqualityCheck,
            alsoNotify);
    }

    private static ImmutableArray<string> EvaluateStringArray(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<string>();

        foreach (var value in EvaluateStringArrayValues(semanticModel, expression, cancellationToken))
        {
            if (!builder.Contains(value))
            {
                builder.Add(value);
            }
        }

        return builder.ToImmutable();
    }

    private static System.Collections.Generic.IEnumerable<string> EvaluateStringArrayValues(
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

    private static System.Collections.Generic.IEnumerable<string> EvaluateCollectionExpression(
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

}
