using CommunityToolkit.Mvvm.ComponentModel;

namespace FinTrack.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentView;

    public MainWindowViewModel()
    {
        ShowLogin();
        FinTrack.Avalonia.Services.CrossPlatformAutoLockService.OnLockTriggered += OnLockTriggered;
    }

    private void OnLockTriggered()
    {
        ShowLogin();
    }

    private void ShowLogin()
    {
        var loginVm = new LoginViewModel();
        loginVm.LoginSuccess += OnLoginSuccess;
        CurrentView = loginVm;
    }

    private void OnLoginSuccess(object? sender, System.EventArgs e)
    {
        if (CurrentView is LoginViewModel loginVm)
        {
            loginVm.LoginSuccess -= OnLoginSuccess;
        }
        
        CurrentView = new MainAppViewModel();
        FinTrack.Avalonia.Services.CrossPlatformAutoLockService.MarkUnlocked();
    }
}
