using System.ComponentModel;
using System.Linq;
using System.Reactive;
using InpcFieldGenerator.Abstractions;
using ReactiveUI;

namespace ReactiveUiSample.ViewModels;

[ReactiveViewModel(NotifyOnChanging = true)]
public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        FirstName = "Ada";
        LastName = "Lovelace";
        ChangingStatus = "Ready.";

        IncrementCommand = ReactiveCommand.Create(() =>
        {
            Counter++;
        });

        PropertyChanging += OnPropertyChanging;
        PropertyChanged += OnPropertyChanged;
    }

    [ReactiveField(AlsoNotify = [nameof(FullName)])]
    public partial string FirstName { get; set; }

    [ReactiveField(AlsoNotify = [nameof(FullName)])]
    public partial string LastName { get; set; }

    [ReactiveField(GenerateEqualityCheck = false, AlsoNotify = [nameof(CounterText)])]
    public partial int Counter { get; set; }

    [ReactiveField(GenerateEqualityCheck = false)]
    public partial string ChangingStatus { get; set; }

    public ReactiveCommand<Unit, Unit> IncrementCommand { get; }

    public string FullName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
            {
                return "Anonymous person";
            }

            return string.Join(' ', new[] { FirstName, LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        }
    }

    public string CounterText => Counter switch
    {
        0 => "No counter increments yet.",
        1 => "Incremented 1 time.",
        _ => $"Incremented {Counter} times."
    };

    private void OnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(ChangingStatus))
        {
            return;
        }

        ChangingStatus = $"{e.PropertyName} changing...";
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChangingStatus))
        {
            return;
        }

        ChangingStatus = $"{e.PropertyName} changed.";
    }
}
