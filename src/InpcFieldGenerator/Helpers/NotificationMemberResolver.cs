using System.Linq;
using Microsoft.CodeAnalysis;

namespace InpcFieldGenerator.Helpers;

internal static class NotificationMemberResolver
{
    internal static IMethodSymbol? FindNotificationMember(INamedTypeSymbol typeSymbol, string memberName)
    {
        for (var current = typeSymbol; current is not null; current = current.BaseType)
        {
            var candidate = current.GetMembers(memberName)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(IsCandidate);

            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsCandidate(IMethodSymbol methodSymbol)
    {
        return methodSymbol is { IsStatic: false, ReturnsVoid: true } and { Parameters.Length: 1 }
            && methodSymbol.Parameters[0].Type.SpecialType == SpecialType.System_String;
    }
}
