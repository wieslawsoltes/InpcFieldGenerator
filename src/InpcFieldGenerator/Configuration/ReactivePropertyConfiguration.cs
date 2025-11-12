using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace InpcFieldGenerator.Configuration;

internal sealed record ReactivePropertyConfiguration(
    string PropertyName,
    string ChangedNotificationMember,
    bool NotifyOnChanging,
    bool GenerateEqualityCheck,
    ImmutableArray<string> AdditionalNotifications)
{
    public static ReactivePropertyConfiguration Create(
        IPropertySymbol propertySymbol,
        ReactiveFieldAttributeSettings propertySettings,
        ReactiveDefaults defaults)
    {
        var propertyName = propertySettings.PropertyName ?? propertySymbol.Name;
        var changedMember = propertySettings.ViewModelMember
            ?? defaults.ViewModelMember
            ?? "RaisePropertyChanged";

        var notifyOnChanging = propertySettings.NotifyOnChanging
            ?? defaults.NotifyOnChanging
            ?? false;

        var generateEqualityCheck = propertySettings.GenerateEqualityCheck
            ?? defaults.GenerateEqualityCheck
            ?? true;

        var builder = ImmutableArray.CreateBuilder<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        AppendValues(builder, seen, defaults.AlsoNotify);
        AppendValues(builder, seen, propertySettings.AlsoNotify);

        // Avoid self notifications.
        var notifications = builder
            .Where(notification => !string.Equals(notification, propertyName, StringComparison.Ordinal))
            .ToImmutableArray();

        return new ReactivePropertyConfiguration(
            propertyName,
            changedMember,
            notifyOnChanging,
            generateEqualityCheck,
            notifications);
    }

    private static void AppendValues(
        ImmutableArray<string>.Builder builder,
        ISet<string> seen,
        ImmutableArray<string> values)
    {
        foreach (var value in values)
        {
            if (seen.Add(value))
            {
                builder.Add(value);
            }
        }
    }
}
