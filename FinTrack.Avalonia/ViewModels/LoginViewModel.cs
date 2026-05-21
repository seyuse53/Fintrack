using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace FinTrack.Avalonia.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<string> _profiles = new();

    [ObservableProperty]
    private string? _selectedProfile;

    [ObservableProperty]
    private bool _isFirstLaunch;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public LoginViewModel()
    {
        LoadProfiles();
    }

    public void LoadProfiles()
    {
        var profilesList = SettingsManager.GetProfiles();
        Profiles.Clear();
        foreach (var p in profilesList) Profiles.Add(p);

        if (Profiles.Any())
        {
            SelectedProfile = Profiles.First();
        }
        else
        {
            // First time ever running the app, no profiles
            IsFirstLaunch = true;
        }
    }

    partial void OnSelectedProfileChanged(string? value)
    {
        if (value != null)
        {
            SettingsManager.SwitchProfile(value);
            IsFirstLaunch = !SettingsManager.IsPasswordSet();
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            HasError = false;
        }
    }

    [RelayCommand]
    private void Login()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            ShowError("Şifre alanı boş bırakılamaz!");
            return;
        }

        if (IsFirstLaunch)
        {
            if (Password != ConfirmPassword)
            {
                ShowError("Şifreler eşleşmiyor.");
                return;
            }

            string recoveryCode = SettingsManager.SetupFirstTimePassword(Password);
            // TODO: Show recovery code to user
            
            OnLoginSuccess();
        }
        else
        {
            if (SettingsManager.VerifyPasswordAndLoadKey(Password))
            {
                OnLoginSuccess();
            }
            else
            {
                ShowError("Hatalı şifre girdiniz.");
                Password = string.Empty;
            }
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    public event System.EventHandler? LoginSuccess;

    private void OnLoginSuccess()
    {
        HasError = false;
        LoginSuccess?.Invoke(this, System.EventArgs.Empty);
    }
}
