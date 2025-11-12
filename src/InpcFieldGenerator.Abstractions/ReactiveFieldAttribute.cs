using System.Diagnostics.CodeAnalysis;

namespace InpcFieldGenerator.Abstractions;

/// <summary>
/// Marks a <c>partial</c> property for INPC generation using the <c>field</c> keyword.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ReactiveFieldAttribute : Attribute
{
    private string[] _alsoNotify = Array.Empty<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveFieldAttribute"/> class.
    /// </summary>
    public ReactiveFieldAttribute()
    {
        GenerateEqualityCheck = true;
    }

    /// <summary>
    /// Gets or sets the emitted property name, overriding the source symbol name.
    /// </summary>
    public string? PropertyName { get; set; }

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
    /// Gets or sets a collection of additional property names to notify when the target property changes.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Attributes require array-backed properties.")]
    public string[] AlsoNotify
    {
        get => _alsoNotify;
        set => _alsoNotify = value is { Length: > 0 } ? value : Array.Empty<string>();
    }

    /// <summary>
    /// Gets or sets a value indicating whether equality guards should be emitted.
    /// </summary>
    public bool GenerateEqualityCheck { get; set; }
}
