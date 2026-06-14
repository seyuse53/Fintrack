using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Models;
using FinTrack.Core.Services;
using FinTrack.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Avalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    // ==================== TAB NAVIGATION ====================

    [ObservableProperty]
    private int _selectedTabIndex;

    // ==================== GENERAL TAB ====================

    [ObservableProperty]
    private List<string> _availableThemes = new() { "Sistem", "Aydınlık", "Karanlık" };

    [ObservableProperty]
    private string _selectedTheme = "Sistem";

    partial void OnSelectedThemeChanged(string value)
    {
        SettingsManager.SetThemePreference(value);
        
        var app = global::Avalonia.Application.Current;
        if (app != null)
        {
            if (value == "Aydınlık")
                app.RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Light;
            else if (value == "Karanlık")
                app.RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Dark;
            else
                app.RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Default;
        }
    }

    // ==================== SECURITY TAB ====================

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _passwordStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasPasswordError;

    [ObservableProperty]
    private bool _isPasswordSuccess;

    [ObservableProperty]
    private bool _autoLockEnabled;

    [ObservableProperty]
    private string _autoLockTimeout = "3";

    // ==================== CATEGORY TAB ====================

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    [ObservableProperty]
    private int _selectedCategoryTypeIndex;

    [ObservableProperty]
    private ObservableCollection<Category> _parentCategories = new();

    [ObservableProperty]
    private Category? _selectedParentCategory;

    [ObservableProperty]
    private ObservableCollection<Category> _visibleCategories = new();

    [ObservableProperty]
    private ObservableCollection<Category> _hiddenCategories = new();

    // ==================== BACKUP TAB ====================

    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private bool _backupEnabled;

    [ObservableProperty]
    private string _backupPath = string.Empty;

    [ObservableProperty]
    private string _maxBackupCount = "5";

    [ObservableProperty]
    private string _maxBackupAge = "30";

    // ==================== API & TAX TAB ====================

    [ObservableProperty]
    private int _dovizProviderIndex;
    partial void OnDovizProviderIndexChanged(int value) => UpdateCustomApiPanelVisibility();

    [ObservableProperty]
    private int _altinProviderIndex;
    partial void OnAltinProviderIndexChanged(int value) => UpdateCustomApiPanelVisibility();

    [ObservableProperty]
    private int _hisseProviderIndex;
    partial void OnHisseProviderIndexChanged(int value) => UpdateCustomApiPanelVisibility();

    [ObservableProperty]
    private int _kriptoProviderIndex;
    partial void OnKriptoProviderIndexChanged(int value) => UpdateCustomApiPanelVisibility();

    [ObservableProperty]
    private int _fonProviderIndex;
    partial void OnFonProviderIndexChanged(int value) => UpdateCustomApiPanelVisibility();

    [ObservableProperty]
    private int _digerProviderIndex;
    partial void OnDigerProviderIndexChanged(int value) => UpdateCustomApiPanelVisibility();

    private void UpdateCustomApiPanelVisibility()
    {
        ShowCustomApiPanel = DovizProviderIndex == (int)ApiProviderType.Custom
                          || AltinProviderIndex == (int)ApiProviderType.Custom
                          || HisseProviderIndex == (int)ApiProviderType.Custom
                          || KriptoProviderIndex == (int)ApiProviderType.Custom
                          || FonProviderIndex == (int)ApiProviderType.Custom
                          || DigerProviderIndex == (int)ApiProviderType.Custom;
    }

    [ObservableProperty]
    private string _customApiUrl = string.Empty;

    [ObservableProperty]
    private bool _showCustomApiPanel;

    [ObservableProperty]
    private string _taxRateHisse = "0";

    [ObservableProperty]
    private string _taxRateMKYO = "10";

    [ObservableProperty]
    private string _taxRateDoviz = "0";

    [ObservableProperty]
    private string _taxRateAltin = "0";

    [ObservableProperty]
    private string _taxRateKripto = "0";

    [ObservableProperty]
    private string _taxRateYurtDisi = "0";

    [ObservableProperty]
    private string _taxRateTemettü = "15";

    // ==================== STATUS MESSAGES ====================

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusError;

    [ObservableProperty]
    private bool _hasStatusSuccess;

    // ==================== EVENTS (for View to handle folder picker / dialogs) ====================

    public event Func<string, string, string, string, System.Threading.Tasks.Task<bool?>>? ShowConfirmDialog;
    public event Func<string, System.Threading.Tasks.Task<string?>>? RequestFolderSelection;

    // ==================== CONSTRUCTOR ====================

    public SettingsViewModel()
    {
        SelectedTabIndex = 0; // Changed later by UI bindings

        LoadData();
    }

    private void LoadData()
    {
        SelectedTheme = SettingsManager.GetThemePreference();
        
        // Security
        try
        {
            var settings = SettingsManager.LoadSettings();

            // Database path
            DatabasePath = SettingsManager.GetDatabasePath();

            // API Providers
            DovizProviderIndex = (int)settings.DovizProvider;
            AltinProviderIndex = (int)settings.AltinProvider;
            HisseProviderIndex = (int)settings.HisseProvider;
            KriptoProviderIndex = (int)settings.KriptoProvider;
            FonProviderIndex = (int)settings.FonProvider;
            DigerProviderIndex = (int)settings.DigerProvider;
            CustomApiUrl = SettingsManager.GetCustomApiUrl() ?? string.Empty;

            bool hasCustom = settings.DovizProvider == ApiProviderType.Custom
                || settings.AltinProvider == ApiProviderType.Custom
                || settings.HisseProvider == ApiProviderType.Custom;
            ShowCustomApiPanel = hasCustom;

            // Backup
            var backupSettings = SettingsManager.GetBackupSettings();
            BackupEnabled = backupSettings.Enabled;
            BackupPath = backupSettings.Directory ?? string.Empty;
            MaxBackupCount = backupSettings.MaxCount.ToString();
            MaxBackupAge = backupSettings.MaxAgeDays.ToString();

            // Auto-Lock
            var autoLockSettings = SettingsManager.GetAutoLockSettings();
            AutoLockEnabled = autoLockSettings.Enabled;
            AutoLockTimeout = autoLockSettings.TimeoutMinutes.ToString();

            // Tax Rates
            TaxRateHisse = settings.TaxRateHisseSenedi.ToString(CultureInfo.InvariantCulture);
            TaxRateMKYO = settings.TaxRateMKYO.ToString(CultureInfo.InvariantCulture);
            TaxRateDoviz = settings.TaxRateDöviz.ToString(CultureInfo.InvariantCulture);
            TaxRateAltin = settings.TaxRateAltın.ToString(CultureInfo.InvariantCulture);
            TaxRateKripto = settings.TaxRateKripto.ToString(CultureInfo.InvariantCulture);
            TaxRateYurtDisi = settings.TaxRateYurtDışı.ToString(CultureInfo.InvariantCulture);
            TaxRateTemettü = settings.TaxRateTemettü.ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            // Settings load failed, use defaults
        }
    }

    // ==================== TAB SWITCHING ====================

    [RelayCommand]
    private void SwitchTab(string indexStr)
    {
        if (int.TryParse(indexStr, out int index))
            SelectedTabIndex = index;
    }

    // ==================== SECURITY COMMANDS ====================

    [RelayCommand]
    private async System.Threading.Tasks.Task ChangePasswordAsync()
    {
        PasswordStatusMessage = "";
        HasPasswordError = false;
        IsPasswordSuccess = false;

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            PasswordStatusMessage = "Lütfen mevcut şifreyi girin.";
            HasPasswordError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 4)
        {
            PasswordStatusMessage = "Yeni şifre en az 4 karakter olmalıdır.";
            HasPasswordError = true;
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            PasswordStatusMessage = "Yeni şifreler eşleşmiyor.";
            HasPasswordError = true;
            return;
        }

        if (!SettingsManager.VerifyPasswordAndLoadKey(CurrentPassword))
        {
            PasswordStatusMessage = "Mevcut şifre yanlış.";
            HasPasswordError = true;
            CurrentPassword = string.Empty;
            return;
        }

        if (SettingsManager.ChangePassword(CurrentPassword, NewPassword))
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Başarılı", "Şifreniz başarıyla değiştirildi. Artık yeni şifrenizle giriş yapabilirsiniz.", "Tamam", "");

            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            PasswordStatusMessage = "";
            IsPasswordSuccess = true;
        }
        else
        {
            PasswordStatusMessage = "Şifre değiştirilemedi (DEK hatası). Lütfen teknik desteğe danışın.";
            HasPasswordError = true;
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SaveAutoLockSettingsAsync()
    {
        if (!int.TryParse(AutoLockTimeout, out int timeoutMin) || timeoutMin < 1)
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Geçersiz Veri", "Kilitlenme süresi (dakika) geçerli bir sayı olmalı ve 1'den büyük olmalıdır.", "Tamam", "");
            return;
        }

        SettingsManager.SaveAutoLockSettings(AutoLockEnabled, timeoutMin);

        if (ShowConfirmDialog != null)
            await ShowConfirmDialog.Invoke("Başarılı", "Otomatik kilitleme ayarları kaydedildi.", "Tamam", "");
    }

    // ==================== CATEGORY COMMANDS ====================

    public void LoadCategories()
    {
        try
        {
            var db = App.Services?.GetService<AppDbContext>();
            if (db == null) return;

            var categories = db.Categories
                .Include(c => c.ParentCategory)
                .Where(c => c.Type != TransactionType.Transfer)
                .ToList();

            VisibleCategories = new ObservableCollection<Category>(
                SortCategoriesHierarchically(categories.Where(c => c.IsVisible).ToList()));
            HiddenCategories = new ObservableCollection<Category>(
                SortCategoriesHierarchically(categories.Where(c => !c.IsVisible).ToList()));

            // Update parent category filter based on selected type
            UpdateParentCategoryFilter();
        }
        catch (Exception)
        {
            // Silently handle
        }
    }

    partial void OnSelectedCategoryTypeIndexChanged(int value)
    {
        UpdateParentCategoryFilter();
    }

    private void UpdateParentCategoryFilter()
    {
        try
        {
            var db = App.Services?.GetService<AppDbContext>();
            if (db == null) return;

            TransactionType selectedType = SelectedCategoryTypeIndex == 1
                ? TransactionType.Income
                : TransactionType.Expense;

            var parents = db.Categories
                .Where(c => c.IsVisible && c.ParentCategoryId == null && c.Type == selectedType)
                .OrderBy(c => c.Name)
                .ToList();

            parents.Insert(0, new Category { Id = 0, Name = "-- Yok (Ana Kategori) --" });

            ParentCategories = new ObservableCollection<Category>(parents);
            SelectedParentCategory = ParentCategories.FirstOrDefault();
        }
        catch (Exception)
        {
            // Silently handle
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task AddCategoryAsync()
    {
        string categoryName = NewCategoryName.Trim();
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Uyarı", "Lütfen bir kategori adı giriniz.", "Tamam", "");
            return;
        }

        TransactionType catType = SelectedCategoryTypeIndex == 1
            ? TransactionType.Income
            : TransactionType.Expense;

        try
        {
            var db = App.Services?.GetService<AppDbContext>();
            if (db == null) return;

            int? parentId = null;
            if (SelectedParentCategory != null && SelectedParentCategory.Id > 0)
            {
                parentId = SelectedParentCategory.Id;
            }

            var newCat = new Category
            {
                Name = categoryName,
                Type = catType,
                IsVisible = true,
                ParentCategoryId = parentId
            };

            db.Categories.Add(newCat);
            db.SaveChanges();

            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Başarılı", $"'{categoryName}' kategorisi başarıyla eklendi.", "Tamam", "");

            NewCategoryName = string.Empty;
            LoadCategories();
        }
        catch (Exception ex)
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Hata", $"Kategori eklenirken hata oluştu: {ex.Message}", "Tamam", "");
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task HideCategoriesAsync(System.Collections.IList? selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0) return;

        var items = selectedItems.Cast<Category>().ToList();

        try
        {
            var db = App.Services?.GetService<AppDbContext>();
            if (db == null) return;

            foreach (var cat in items)
            {
                var dbCat = db.Categories.Find(cat.Id);
                if (dbCat != null)
                {
                    dbCat.IsVisible = false;
                }
            }
            db.SaveChanges();
            LoadCategories();
        }
        catch (Exception ex)
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Hata", $"Kategori gizlenirken hata oluştu: {ex.Message}", "Tamam", "");
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ShowCategoriesAsync(System.Collections.IList? selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0) return;

        var items = selectedItems.Cast<Category>().ToList();

        try
        {
            var db = App.Services?.GetService<AppDbContext>();
            if (db == null) return;

            foreach (var cat in items)
            {
                var dbCat = db.Categories.Find(cat.Id);
                if (dbCat != null)
                {
                    dbCat.IsVisible = true;
                }
            }
            db.SaveChanges();
            LoadCategories();
        }
        catch (Exception ex)
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Hata", $"Kategori gösterilirken hata oluştu: {ex.Message}", "Tamam", "");
        }
    }

    // ==================== BACKUP COMMANDS ====================

    [RelayCommand]
    private async System.Threading.Tasks.Task SelectDatabaseFolderAsync()
    {
        if (RequestFolderSelection != null)
        {
            string? folder = await RequestFolderSelection.Invoke("Bulut Senkronizasyon Klasörü Seçin");
            if (!string.IsNullOrEmpty(folder))
            {
                string currentPath = SettingsManager.GetDatabasePath();
                string fileName = System.IO.Path.GetFileName(currentPath);
                DatabasePath = System.IO.Path.Combine(folder, fileName);
            }
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SaveCloudSettingsAsync()
    {
        string currentPath = SettingsManager.GetDatabasePath();

        if (string.IsNullOrWhiteSpace(DatabasePath) || currentPath.Equals(DatabasePath, StringComparison.OrdinalIgnoreCase))
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Bilgi", "Veritabanı zaten seçili konumda veya yeni bir klasör seçilmedi.", "Tamam", "");
            return;
        }

        try
        {
            // Confirm move
            if (ShowConfirmDialog != null)
            {
                var confirmed = await ShowConfirmDialog.Invoke(
                    "Veritabanını Taşı",
                    $"Veritabanı dosyası şu konuma taşınacak:\n\n{DatabasePath}\n\nDevam etmek istiyor musunuz?",
                    "Evet, Taşı",
                    "Vazgeç");

                if (confirmed != true) return;
            }

            if (System.IO.File.Exists(DatabasePath))
            {
                if (ShowConfirmDialog != null)
                {
                    var conflictConfirmed = await ShowConfirmDialog.Invoke(
                        "Dosya Çakışması",
                        "Hedef klasörde zaten bir fintrack.db dosyası var. Mevcut dosyayı SEÇİLEN klasördeki ile değiştirmek ister misiniz?",
                        "Değiştir",
                        "Vazgeç");

                    if (conflictConfirmed != true) return;
                }
            }
            else
            {
                System.IO.File.Copy(currentPath, DatabasePath, true);
            }

            SettingsManager.SetDatabasePath(DatabasePath);

            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Başarılı", "Veritabanı konumu başarıyla güncellendi!\n\nDeğişikliklerin tam olarak uygulanması için lütfen uygulamayı kapatıp tekrar açın.", "Tamam", "");
        }
        catch (Exception ex)
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Hata", $"Dosya taşıma sırasında bir hata oluştu: {ex.Message}", "Tamam", "");
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SelectBackupFolderAsync()
    {
        if (RequestFolderSelection != null)
        {
            string? folder = await RequestFolderSelection.Invoke("Otomatik Yedekleme Klasörü Seçin");
            if (!string.IsNullOrEmpty(folder))
            {
                BackupPath = folder;
            }
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SaveBackupSettingsAsync()
    {
        if (BackupEnabled && string.IsNullOrWhiteSpace(BackupPath))
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Eksik Bilgi", "Otomatik yedekleme açıkken lütfen bir yedekleme klasörü seçin.", "Tamam", "");
            return;
        }

        if (!int.TryParse(MaxBackupCount, out int maxCount) || maxCount < 1)
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Geçersiz Veri", "Maksimum yedek sayısı geçerli bir sayı olmalı ve 1'den büyük olmalıdır.", "Tamam", "");
            return;
        }

        if (!int.TryParse(MaxBackupAge, out int maxAgeDays) || maxAgeDays < 1)
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Geçersiz Veri", "Maksimum saklama günü geçerli bir sayı olmalı ve 1'den büyük olmalıdır.", "Tamam", "");
            return;
        }

        SettingsManager.SaveBackupSettings(BackupEnabled, BackupPath, maxCount, maxAgeDays);

        if (ShowConfirmDialog != null)
            await ShowConfirmDialog.Invoke("Başarılı", "Yedekleme ayarları başarıyla kaydedildi.", "Tamam", "");
    }

    // ==================== API & TAX COMMANDS ====================

    [RelayCommand]
    private async System.Threading.Tasks.Task SaveApiSettingsAsync()
    {
        var doviz = (ApiProviderType)DovizProviderIndex;
        var altin = (ApiProviderType)AltinProviderIndex;
        var hisse = (ApiProviderType)HisseProviderIndex;
        var kripto = (ApiProviderType)KriptoProviderIndex;
        var fon = (ApiProviderType)FonProviderIndex;
        var diger = (ApiProviderType)DigerProviderIndex;

        SettingsManager.SaveCategoryProviders(doviz, altin, hisse, kripto, fon, diger);

        bool hasCustom = doviz == ApiProviderType.Custom || altin == ApiProviderType.Custom
            || hisse == ApiProviderType.Custom || kripto == ApiProviderType.Custom;
        if (hasCustom)
        {
            SettingsManager.SetCustomApiUrl(CustomApiUrl);
        }

        ShowCustomApiPanel = hasCustom;

        if (ShowConfirmDialog != null)
            await ShowConfirmDialog.Invoke("Başarılı", "API ayarları başarıyla kaydedildi.\n\nYatırımlar sayfasında 'Fiyatları Çek' butonuyla yeni ayarları test edebilirsiniz.", "Tamam", "");
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SaveTaxSettingsAsync()
    {
        try
        {
            decimal ParseTaxRate(string text)
            {
                if (decimal.TryParse(text.Replace(",", "."),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal val))
                    return val;
                return 0;
            }

            SettingsManager.SaveTaxSettings(
                hisse: ParseTaxRate(TaxRateHisse),
                mkyo: ParseTaxRate(TaxRateMKYO),
                temettü: ParseTaxRate(TaxRateTemettü),
                döviz: ParseTaxRate(TaxRateDoviz),
                altın: ParseTaxRate(TaxRateAltin),
                kripto: ParseTaxRate(TaxRateKripto),
                yurtDışı: ParseTaxRate(TaxRateYurtDisi)
            );

            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Başarılı", "Vergi oranları başarıyla kaydedildi.", "Tamam", "");
        }
        catch (Exception ex)
        {
            if (ShowConfirmDialog != null)
                await ShowConfirmDialog.Invoke("Hata", $"Vergi ayarları kaydedilirken hata oluştu: {ex.Message}", "Tamam", "");
        }
    }

    // ==================== HELPERS ====================

    private static List<Category> SortCategoriesHierarchically(List<Category> flatList)
    {
        var result = new List<Category>();

        var types = flatList.Select(c => c.Type).Distinct().OrderBy(t => t);

        foreach (var type in types)
        {
            var typeCategories = flatList.Where(c => c.Type == type).ToList();
            var parents = typeCategories.Where(c => c.ParentCategoryId == null).OrderBy(c => c.Name).ToList();

            foreach (var parent in parents)
            {
                result.Add(parent);
                var children = typeCategories.Where(c => c.ParentCategoryId == parent.Id).OrderBy(c => c.Name).ToList();
                result.AddRange(children);
            }

            // Orphans
            var orphans = typeCategories
                .Where(c => c.ParentCategoryId != null && !parents.Any(p => p.Id == c.ParentCategoryId))
                .OrderBy(c => c.Name).ToList();
            result.AddRange(orphans);
        }

        return result;
    }
}
