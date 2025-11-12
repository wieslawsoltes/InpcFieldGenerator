using System.ComponentModel;
using ReactiveUI;

namespace ReactiveUiSample.ViewModels;

/// <summary>
/// Base view model exposing string-based notification helpers for the source generator.
/// </summary>
public abstract class ViewModelBase : ReactiveObject
{
    protected void RaisePropertyChanged(string propertyName)
    {
        ((IReactiveObject)this).RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    protected void RaisePropertyChanging(string propertyName)
    {
        ((IReactiveObject)this).RaisePropertyChanging(new PropertyChangingEventArgs(propertyName));
    }
}
