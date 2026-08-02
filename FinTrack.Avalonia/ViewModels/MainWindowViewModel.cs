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
        FinTrack.Core.Services.SettingsManager.ClearActiveKey();
        ShowLogin();
    }

    private void ShowLogin(string? selectProfile = null)
    {
        var loginVm = new LoginViewModel();
        if (!string.IsNullOrEmpty(selectProfile))
        {
            loginVm.LoadProfiles(selectProfile);
        }
        loginVm.LoginSuccess += OnLoginSuccess;
        loginVm.AddProfileRequested += OnAddProfileRequested;
        CurrentView = loginVm;
    }

    private void OnAddProfileRequested(object? sender, System.EventArgs e)
    {
        if (CurrentView is LoginViewModel loginVm)
        {
            loginVm.LoginSuccess -= OnLoginSuccess;
            loginVm.AddProfileRequested -= OnAddProfileRequested;
        }

        var addProfileVm = new AddProfileViewModel();
        addProfileVm.OnComplete += OnAddProfileComplete;
        CurrentView = addProfileVm;
    }

    private void OnAddProfileComplete(object? sender, System.EventArgs e)
    {
        string? createdProfileName = null;
        if (CurrentView is AddProfileViewModel addProfileVm)
        {
            addProfileVm.OnComplete -= OnAddProfileComplete;
            // Oluşturulan profil adını al (boşsa iptal edilmiş demektir)
            if (!string.IsNullOrWhiteSpace(addProfileVm.ProfileName))
            {
                createdProfileName = addProfileVm.ProfileName;
            }
        }

        ShowLogin(createdProfileName);
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
