using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        try
        {
            var context = App.Services?.GetService<FinTrack.Data.AppDbContext>();
            if (context != null)
            {
                // SQLCipher and EF Core migrations often conflict, or migration history gets out of sync.
                // We manually ensure the columns exist here if EF skipped them.
                bool hasGramGold = false;
                bool hasCustomCurrentValue = false;
                bool hasCustomStateContribution = false;
                bool hasBesStartDate = false;
                bool hasBesRetirementDate = false;
                bool hasBesContractNo = false;
                bool hasParticipantBirthDate = false;
                bool hasBesRetirementAge = false;

                using (var command = context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "PRAGMA table_info(InvestmentTransactions);";
                    context.Database.OpenConnection();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.GetString(1) == "GramGoldEquivalent") hasGramGold = true;
                        }
                    }

                    command.CommandText = "PRAGMA table_info(InvestmentAssets);";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var colName = reader.GetString(1);
                            if (colName == "CustomCurrentValue") hasCustomCurrentValue = true;
                            if (colName == "CustomStateContribution") hasCustomStateContribution = true;
                            if (colName == "BesStartDate") hasBesStartDate = true;
                            if (colName == "BesRetirementDate") hasBesRetirementDate = true;
                            if (colName == "BesContractNo") hasBesContractNo = true;
                            if (colName == "ParticipantBirthDate") hasParticipantBirthDate = true;
                            if (colName == "BesRetirementAge") hasBesRetirementAge = true;
                        }
                    }
                }
                
                if (!hasGramGold)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentTransactions ADD COLUMN GramGoldEquivalent TEXT;");
                }
                if (!hasCustomCurrentValue)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN CustomCurrentValue TEXT;");
                }
                if (!hasCustomStateContribution)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN CustomStateContribution TEXT;");
                }
                if (!hasBesStartDate)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN BesStartDate TEXT;");
                }
                if (!hasBesRetirementDate)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN BesRetirementDate TEXT;");
                }
                if (!hasBesContractNo)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN BesContractNo TEXT;");
                }
                if (!hasParticipantBirthDate)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN ParticipantBirthDate TEXT;");
                }
                if (!hasBesRetirementAge)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN BesRetirementAge INTEGER;");
                }
                
                context.Database.Migrate();
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Migration failed: " + ex.Message);
        }

        LoginSuccess?.Invoke(this, System.EventArgs.Empty);
    }
}
