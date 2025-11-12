using Microsoft.CodeAnalysis;

namespace InpcFieldGenerator.Diagnostics;

internal static class DiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor PropertyMustBePartial = new(
        id: "INPCFG001",
        title: "Reactive field property must be partial",
        messageFormat: "Property '{0}' must be declared as a partial property to participate in generation",
        category: "InpcFieldGenerator.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Reactive field properties must be declared with the 'partial' modifier introduced in C# 14.");

    internal static readonly DiagnosticDescriptor PropertyMustHaveSetter = new(
        id: "INPCFG002",
        title: "Reactive field property must have a setter",
        messageFormat: "Property '{0}' must declare an accessible setter to participate in generation",
        category: "InpcFieldGenerator.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Reactive field properties must expose a setter so the generator can assign the backing field.");

    internal static readonly DiagnosticDescriptor NotificationMemberMissing = new(
        id: "INPCFG003",
        title: "Notification member not found",
        messageFormat: "Type '{0}' must contain an accessible method '{1}(string)' to raise property change notifications",
        category: "InpcFieldGenerator.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Reactive field generation requires a notification method (for example, RaisePropertyChanged) taking a string argument.");

    internal static readonly DiagnosticDescriptor ChangingNotificationMemberMissing = new(
        id: "INPCFG004",
        title: "Changing notification member not found",
        messageFormat: "Type '{0}' must contain an accessible method '{1}(string)' to raise property changing notifications when NotifyOnChanging is enabled",
        category: "InpcFieldGenerator.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "RaisePropertyChanging (or the configured member) must be available when NotifyOnChanging is requested.");

    internal static readonly DiagnosticDescriptor AlsoNotifyPropertyMissing = new(
        id: "INPCFG005",
        title: "AlsoNotify property not found",
        messageFormat: "Property '{0}' declares AlsoNotify target '{1}' which was not found on type '{2}' or its base types",
        category: "InpcFieldGenerator.Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "AlsoNotify values must map to existing properties.");
}
