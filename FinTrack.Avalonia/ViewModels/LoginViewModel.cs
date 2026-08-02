using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FinTrack.Avalonia.Localization;
using FinTrack.Core.Helpers;

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

    [ObservableProperty]
    private string _infoMessage = string.Empty;

    [ObservableProperty]
    private bool _hasInfo;

    [ObservableProperty]
    private bool _isForgotPasswordVisible;

    [ObservableProperty]
    private string _recoveryCode = string.Empty;

    [ObservableProperty]
    private string _newRecoveryPassword = string.Empty;

    [ObservableProperty]
    private string _confirmNewRecoveryPassword = string.Empty;

    [ObservableProperty]
    private bool _isDeleteProfileVisible;

    [ObservableProperty]
    private string _deleteProfilePassword = string.Empty;

    [ObservableProperty]
    private bool _isRemoveProfileVisible;

    [ObservableProperty]
    private bool _isRecoveryCodeVisible;

    [ObservableProperty]
    private string _recoveryCodeDisplay = string.Empty;

    public event System.EventHandler? AddProfileRequested;

    public LoginViewModel()
    {
        LoadProfiles();
    }

    public void LoadProfiles(string? selectProfile = null)
    {
        var profilesList = SettingsManager.GetProfiles();
        Profiles.Clear();
        foreach (var p in profilesList) Profiles.Add(p);

        if (Profiles.Any())
        {
            // Belirli bir profil isteniyorsa ve listede varsa onu seç
            if (!string.IsNullOrEmpty(selectProfile) && Profiles.Contains(selectProfile))
            {
                SelectedProfile = selectProfile;
            }
            else
            {
                SelectedProfile = Profiles.First();
            }
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
            HasInfo = false;
            IsForgotPasswordVisible = false;
            IsDeleteProfileVisible = false;
        }
    }

    [RelayCommand]
    private void AddProfile()
    {
        AddProfileRequested?.Invoke(this, System.EventArgs.Empty);
    }

    [RelayCommand]
    private void RemoveProfile()
    {
        AppLogger.Info($"[LoginViewModel] RemoveProfile clicked. SelectedProfile: {SelectedProfile}");
        if (string.IsNullOrEmpty(SelectedProfile)) return;
        IsRemoveProfileVisible = true;
        IsDeleteProfileVisible = false;
        IsForgotPasswordVisible = false;
        HasError = false;
        HasInfo = false;
        AppLogger.Info($"[LoginViewModel] IsRemoveProfileVisible set to {IsRemoveProfileVisible}");
    }

    [RelayCommand]
    private void CancelRemoveProfile()
    {
        IsRemoveProfileVisible = false;
    }

    [RelayCommand]
    private void ConfirmRemoveProfile()
    {
        AppLogger.Info($"[LoginViewModel] ConfirmRemoveProfile clicked.");
        if (!string.IsNullOrEmpty(SelectedProfile))
        {
            string removedProfile = SelectedProfile;
            SettingsManager.DeleteProfile(SelectedProfile, deleteDatabase: false);
            IsRemoveProfileVisible = false;
            LoadProfiles();
            ShowInfo(LocalizationService.GetString("Login_Msg_ProfileRemoved").Replace("{0}", removedProfile));
        }
    }

    [RelayCommand]
    private void ForgotPassword()
    {
        if (IsFirstLaunch) return;
        IsForgotPasswordVisible = true;
        IsDeleteProfileVisible = false;
        RecoveryCode = string.Empty;
        NewRecoveryPassword = string.Empty;
        ConfirmNewRecoveryPassword = string.Empty;
        HasError = false;
    }

    [RelayCommand]
    private void CancelForgotPassword()
    {
        IsForgotPasswordVisible = false;
        HasError = false;
    }

    [RelayCommand]
    private void ResetPassword()
    {
        if (string.IsNullOrWhiteSpace(RecoveryCode) || string.IsNullOrWhiteSpace(NewRecoveryPassword))
        {
            ShowError(LocalizationService.GetString("Login_Msg_FillAllFields"));
            return;
        }

        if (NewRecoveryPassword != ConfirmNewRecoveryPassword)
        {
            ShowError(LocalizationService.GetString("Login_Msg_PasswordsDoNotMatch"));
            return;
        }

        bool success = SettingsManager.ResetPasswordWithRecoveryCode(RecoveryCode, NewRecoveryPassword);
        if (success)
        {
            ShowInfo(LocalizationService.GetString("Login_Msg_PasswordResetSuccess"));
            IsForgotPasswordVisible = false;
            RecoveryCode = string.Empty;
            NewRecoveryPassword = string.Empty;
            ConfirmNewRecoveryPassword = string.Empty;
            OnLoginSuccess();
        }
        else
        {
            ShowError(LocalizationService.GetString("Login_Msg_PasswordResetFailed"));
        }
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        AppLogger.Info($"[LoginViewModel] DeleteProfile clicked. SelectedProfile: {SelectedProfile}, IsFirstLaunch: {IsFirstLaunch}");
        if (string.IsNullOrEmpty(SelectedProfile) || IsFirstLaunch) return;
        
        IsDeleteProfileVisible = true;
        IsRemoveProfileVisible = false;
        IsForgotPasswordVisible = false;
        DeleteProfilePassword = string.Empty;
        HasError = false;
        HasInfo = false;
        AppLogger.Info($"[LoginViewModel] IsDeleteProfileVisible set to {IsDeleteProfileVisible}");
    }

    [RelayCommand]
    private void CancelDeleteProfile()
    {
        IsDeleteProfileVisible = false;
        HasError = false;
    }

    [RelayCommand]
    private void ConfirmDeleteProfile()
    {
        AppLogger.Info($"[LoginViewModel] ConfirmDeleteProfile clicked.");
        if (string.IsNullOrWhiteSpace(DeleteProfilePassword))
        {
            ShowError(LocalizationService.GetString("Login_Msg_EnterPassword"));
            return;
        }

        if (SettingsManager.VerifyPasswordAndLoadKey(DeleteProfilePassword))
        {
            string profileToDelete = SelectedProfile!;
            SettingsManager.DeleteProfile(profileToDelete, deleteDatabase: true);
            IsDeleteProfileVisible = false;
            LoadProfiles();
            ShowInfo(LocalizationService.GetString("Login_Msg_ProfileDeleted").Replace("{0}", profileToDelete));
        }
        else
        {
            ShowError(LocalizationService.GetString("Login_Msg_IncorrectPassword"));
        }
    }

    [RelayCommand]
    private void Login()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            ShowError(LocalizationService.GetString("Login_Msg_PasswordEmpty"));
            return;
        }

        if (IsFirstLaunch)
        {
            if (Password != ConfirmPassword)
            {
                ShowError(LocalizationService.GetString("Login_Msg_PasswordsDoNotMatch"));
                return;
            }

            if (Password.Length < 4)
            {
                ShowError(LocalizationService.GetString("Login_Msg_PasswordMinLength"));
                return;
            }

            string recoveryCode = SettingsManager.SetupFirstTimePassword(Password);
            
            // Kurtarma kodunu kullanıcıya göster, onay alındıktan sonra giriş yapılacak
            RecoveryCodeDisplay = recoveryCode;
            IsRecoveryCodeVisible = true;
            HasError = false;
            HasInfo = false;
        }
        else
        {
            if (SettingsManager.VerifyPasswordAndLoadKey(Password))
            {
                OnLoginSuccess();
            }
            else
            {
                ShowError(LocalizationService.GetString("Login_Msg_IncorrectPassword"));
                Password = string.Empty;
            }
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        HasInfo = false;
    }

    private void ShowInfo(string message)
    {
        InfoMessage = message;
        HasInfo = true;
        HasError = false;
    }

    [RelayCommand]
    private void AcknowledgeRecoveryCode()
    {
        IsRecoveryCodeVisible = false;
        RecoveryCodeDisplay = string.Empty;
        OnLoginSuccess();
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        LocalizationService.ToggleLanguage();
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
                // First try standard EF Core migration
                try
                {
                    context.Database.Migrate();
                    ShowInfo(LocalizationService.GetString("Login_Msg_SetupCompleted"));
                }
                catch (System.Exception ex)
                {
                    ShowError(ex.Message);
                    AppLogger.Error("EF Migrate failed: " + ex.Message);
                    // Fallback for completely new/empty databases where Migrate() fails due to SQLCipher connection issues
                    try
                    {
                        bool hasCategories = false;
                        using (var command = context.Database.GetDbConnection().CreateCommand())
                        {
                            if (context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                                context.Database.OpenConnection();
                            command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='Categories';";
                            hasCategories = command.ExecuteScalar() != null;
                        }

                        if (!hasCategories)
                        {
                            context.Database.EnsureCreated();
                        }
                    }
                    catch (System.Exception fallbackEx)
                    {
                        AppLogger.Error("EnsureCreated fallback failed: " + fallbackEx.Message);
                    }
                }

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
                
                bool hasInvestmentTransactionsTable = false;
                bool hasInvestmentAssetsTable = false;

                using (var command = context.Database.GetDbConnection().CreateCommand())
                {
                    if (context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                        context.Database.OpenConnection();
                    
                    command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='InvestmentTransactions';";
                    hasInvestmentTransactionsTable = command.ExecuteScalar() != null;
                    
                    command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='InvestmentAssets';";
                    hasInvestmentAssetsTable = command.ExecuteScalar() != null;

                    bool hasPriceHistoriesTable = false;
                    command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='PriceHistories';";
                    hasPriceHistoriesTable = command.ExecuteScalar() != null;

                    if (!hasInvestmentAssetsTable)
                    {
                        context.Database.ExecuteSqlRaw(@"
CREATE TABLE ""InvestmentAssets"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_InvestmentAssets"" PRIMARY KEY AUTOINCREMENT,
    ""Name"" TEXT NOT NULL,
    ""Symbol"" TEXT NOT NULL,
    ""Category"" TEXT NULL,
    ""TotalAmount"" TEXT NOT NULL,
    ""AverageCost"" TEXT NOT NULL,
    ""LastKnownPrice"" TEXT NOT NULL DEFAULT '0',
    ""LastPriceUpdate"" TEXT NULL,
    ""CustomCurrentValue"" TEXT NULL,
    ""CustomStateContribution"" TEXT NULL,
    ""BesStartDate"" TEXT NULL,
    ""BesRetirementDate"" TEXT NULL,
    ""BesContractNo"" TEXT NULL,
    ""ParticipantBirthDate"" TEXT NULL,
    ""BesRetirementAge"" INTEGER NULL
);");
                        hasInvestmentAssetsTable = true;
                    }

                    if (!hasInvestmentTransactionsTable)
                    {
                        context.Database.ExecuteSqlRaw(@"
CREATE TABLE ""InvestmentTransactions"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_InvestmentTransactions"" PRIMARY KEY AUTOINCREMENT,
    ""InvestmentAssetId"" INTEGER NOT NULL,
    ""Type"" INTEGER NOT NULL,
    ""Amount"" TEXT NOT NULL,
    ""UnitPrice"" TEXT NOT NULL,
    ""Fee"" TEXT NOT NULL DEFAULT '0',
    ""TotalCost"" TEXT NOT NULL,
    ""GramGoldEquivalent"" TEXT NULL,
    ""Date"" TEXT NOT NULL,
    ""Notes"" TEXT NULL,
    ""LinkedBankAccountId"" INTEGER NULL,
    ""LinkedCreditCardAccountId"" INTEGER NULL,
    CONSTRAINT ""FK_InvestmentTransactions_BankAccounts_LinkedBankAccountId"" FOREIGN KEY (""LinkedBankAccountId"") REFERENCES ""BankAccounts"" (""Id"") ON DELETE SET NULL,
    CONSTRAINT ""FK_InvestmentTransactions_CreditCardAccounts_LinkedCreditCardAccountId"" FOREIGN KEY (""LinkedCreditCardAccountId"") REFERENCES ""CreditCardAccounts"" (""Id"") ON DELETE SET NULL,
    CONSTRAINT ""FK_InvestmentTransactions_InvestmentAssets_InvestmentAssetId"" FOREIGN KEY (""InvestmentAssetId"") REFERENCES ""InvestmentAssets"" (""Id"") ON DELETE CASCADE
);
CREATE INDEX ""IX_InvestmentTransactions_InvestmentAssetId"" ON ""InvestmentTransactions"" (""InvestmentAssetId"");
CREATE INDEX ""IX_InvestmentTransactions_LinkedBankAccountId"" ON ""InvestmentTransactions"" (""LinkedBankAccountId"");
CREATE INDEX ""IX_InvestmentTransactions_LinkedCreditCardAccountId"" ON ""InvestmentTransactions"" (""LinkedCreditCardAccountId"");
");
                        hasInvestmentTransactionsTable = true;
                        hasGramGold = true;
                    }

                    if (!hasPriceHistoriesTable)
                    {
                        context.Database.ExecuteSqlRaw(@"
CREATE TABLE ""PriceHistories"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PriceHistories"" PRIMARY KEY AUTOINCREMENT,
    ""Symbol"" TEXT NOT NULL,
    ""ClosePrice"" TEXT NOT NULL,
    ""LowPrice"" TEXT NOT NULL,
    ""HighPrice"" TEXT NOT NULL,
    ""Date"" TEXT NOT NULL,
    ""Source"" TEXT NOT NULL
);
CREATE UNIQUE INDEX ""IX_PriceHistories_Symbol_Date"" ON ""PriceHistories"" (""Symbol"", ""Date"");
");
                    }

                    if (hasInvestmentTransactionsTable && !hasGramGold)
                    {
                        command.CommandText = "PRAGMA table_info(InvestmentTransactions);";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader.GetString(1) == "GramGoldEquivalent") hasGramGold = true;
                            }
                        }
                    }

                    if (hasInvestmentAssetsTable)
                    {
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
                }
                
                if (hasInvestmentTransactionsTable && !hasGramGold)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentTransactions ADD COLUMN GramGoldEquivalent TEXT;");
                }
                
                if (hasInvestmentAssetsTable)
                {
                    if (!hasCustomCurrentValue)
                        context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN CustomCurrentValue TEXT;");
                    if (!hasCustomStateContribution)
                        context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN CustomStateContribution TEXT;");
                    if (!hasBesStartDate)
                        context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN BesStartDate TEXT;");
                    if (!hasBesRetirementDate)
                        context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN BesRetirementDate TEXT;");
                    if (!hasBesContractNo)
                        context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN BesContractNo TEXT;");
                    if (!hasParticipantBirthDate)
                        context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN ParticipantBirthDate TEXT;");
                    if (!hasBesRetirementAge)
                        context.Database.ExecuteSqlRaw("ALTER TABLE InvestmentAssets ADD COLUMN BesRetirementAge INTEGER;");
                }
                
                FinTrack.Data.AppDbContext.MigrateInitialBalances(context);
                FinTrack.Data.AppDbContext.DecryptBankNames(context);
            }
        }
        catch (System.Exception ex)
        {
            AppLogger.Error("Migration failed: " + ex.Message);
        }

        LoginSuccess?.Invoke(this, System.EventArgs.Empty);
    }
}
