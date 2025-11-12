namespace InpcFieldGenerator.Abstractions;

/// <summary>
/// Declares class-level defaults for INPC generation on ReactiveUI view models.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ReactiveViewModelAttribute : Attribute
{
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
}
