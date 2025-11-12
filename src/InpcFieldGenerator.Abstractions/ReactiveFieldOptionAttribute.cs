using System.Diagnostics.CodeAnalysis;

namespace InpcFieldGenerator.Abstractions;

/// <summary>
/// Declares assembly-level defaults for the INPC field generator.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ReactiveFieldOptionAttribute : Attribute
{
    private string[] _alsoNotify = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the member on the view model responsible for raising notifications.
    /// Defaults to <c>RaisePropertyChanged</c>.
    /// </summary>
    public string? ViewModelMember { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <c>RaisePropertyChanging</c> should be invoked before assignment.
    /// </summary>
    public bool NotifyOnChanging { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether equality guards should be emitted.
    /// </summary>
    public bool GenerateEqualityCheck { get; set; }

    /// <summary>
    /// Gets or sets additional property names to notify when any generated property changes.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Attributes require array-backed properties.")]
    public string[] AlsoNotify
    {
        get => _alsoNotify;
        set => _alsoNotify = value is { Length: > 0 } ? value : Array.Empty<string>();
    }
}
