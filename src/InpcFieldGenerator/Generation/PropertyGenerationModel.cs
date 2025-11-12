using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InpcFieldGenerator.Generation;

internal sealed record PropertyGenerationModel(
    IPropertySymbol PropertySymbol,
    string PropertyName,
    string PropertyTypeDisplay,
    string ChangedNotificationMember,
    string? ChangingNotificationMember,
    bool NotifyOnChanging,
    bool GenerateEqualityCheck,
    ImmutableArray<string> AdditionalNotifications);
