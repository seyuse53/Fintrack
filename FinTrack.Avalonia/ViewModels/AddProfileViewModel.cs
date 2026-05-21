using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Services;
using System;
using System.Linq;

namespace FinTrack.Avalonia.ViewModels;

public partial class AddProfileViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _profileName = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            ErrorMessage = "Profil adı boş olamaz.";
            HasError = true;
            return;
        }

        var existingProfiles = FinTrack.Core.Services.SettingsManager.GetProfiles();
        if (existingProfiles.Contains(ProfileName, System.StringComparer.OrdinalIgnoreCase))
        {
            ErrorMessage = $"'{ProfileName}' isminde bir profil zaten mevcut. Lütfen farklı bir profil ismi belirleyin.";
            HasError = true;
            return;
        }

        // For simplicity, we just create standard path here, 
        // a file picker can be added later if needed.
        SettingsManager.CreateProfile(ProfileName, null);
        
        // TODO: Navigate back to LoginView or Signal completion
        HasError = false;
    }
}
