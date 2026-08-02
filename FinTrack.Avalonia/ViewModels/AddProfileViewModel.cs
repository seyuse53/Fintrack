using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Services;
using System;
using System.Linq;
using FinTrack.Avalonia.Localization;

namespace FinTrack.Avalonia.ViewModels;

public partial class AddProfileViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _profileName = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public event EventHandler? OnComplete;

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            ErrorMessage = LocalizationService.GetString("AddProfile_ErrorEmpty");
            HasError = true;
            return;
        }

        var existingProfiles = FinTrack.Core.Services.SettingsManager.GetProfiles();
        if (existingProfiles.Contains(ProfileName, System.StringComparer.OrdinalIgnoreCase))
        {
            ErrorMessage = string.Format(LocalizationService.GetString("AddProfile_ErrorExists"), ProfileName);
            HasError = true;
            return;
        }

        // For simplicity, we just create standard path here, 
        // a file picker can be added later if needed.
        SettingsManager.CreateProfile(ProfileName, null);
        
        HasError = false;
        OnComplete?.Invoke(this, EventArgs.Empty);
    }

    public void LinkExistingProfile(string dbPath)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(dbPath);
        string newProfileName = fileName.StartsWith("fintrack_") ? fileName.Substring(9) : fileName;
        
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            ProfileName = newProfileName;
        }

        var existingProfiles = SettingsManager.GetProfiles();
        if (existingProfiles.Contains(ProfileName, StringComparer.OrdinalIgnoreCase))
        {
            ErrorMessage = string.Format(LocalizationService.GetString("AddProfile_ErrorExists"), ProfileName);
            HasError = true;
            return;
        }

        SettingsManager.CreateProfile(ProfileName, dbPath);
        
        HasError = false;
        OnComplete?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        OnComplete?.Invoke(this, EventArgs.Empty);
    }
}
