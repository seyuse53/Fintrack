using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FinTrack.Avalonia.ViewModels;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    public virtual void Dispose()
    {
        // Derived classes can override this to dispose unmanaged resources or DbContexts
    }
}
