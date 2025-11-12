using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace InpcFieldGenerator.Generation;

internal static class EmitterUtilities
{
    internal static string FormatType(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    internal static string FormatTypeParameterList(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        return "<" + string.Join(", ", typeSymbol.TypeParameters.Select(parameter => parameter.Name)) + ">";
    }

    internal static string FormatTypeConstraints(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        var clauses = new List<string>();

        foreach (var parameter in typeSymbol.TypeParameters)
        {
            var parts = new List<string>();

            if (parameter.HasNotNullConstraint)
            {
                parts.Add("notnull");
            }

            if (parameter.HasReferenceTypeConstraint)
            {
                parts.Add("class");
            }

            if (parameter.HasUnmanagedTypeConstraint)
            {
                parts.Add("unmanaged");
            }

            if (parameter.HasValueTypeConstraint)
            {
                parts.Add("struct");
            }

            foreach (var constraintType in parameter.ConstraintTypes)
            {
                parts.Add(FormatType(constraintType));
            }

            if (parameter.HasConstructorConstraint)
            {
                parts.Add("new()");
            }

            if (parts.Count > 0)
            {
                clauses.Add($"where {parameter.Name} : {string.Join(", ", parts)}");
            }
        }

        return clauses.Count == 0 ? string.Empty : " " + string.Join(" ", clauses);
    }

    internal static string FormatAccessibility(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Protected => "protected",
        Accessibility.Private => "private",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => "internal",
    };

    internal static string FormatTypeKeyword(INamedTypeSymbol symbol)
    {
        if (symbol.IsRecord)
        {
            return symbol.TypeKind == TypeKind.Struct ? "record struct" : "record";
        }

        return symbol.TypeKind switch
        {
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            TypeKind.Enum => "enum",
            _ => "class",
        };
    }

    internal static string CreateStringLiteral(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');

        foreach (var character in value)
        {
            switch (character)
            {
                case '\"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\0':
                    builder.Append("\\0");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    internal static string FormatNamespace(INamespaceSymbol namespaceSymbol)
    {
        var text = namespaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return text.StartsWith("global::", System.StringComparison.Ordinal)
            ? text.Substring("global::".Length)
            : text;
    }
}
