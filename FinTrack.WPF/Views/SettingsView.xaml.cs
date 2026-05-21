using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace FinTrack.WPF.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            PathTextBox.Text = SettingsManager.GetDatabasePath();
            
            // Load API Provider (Kategori Bazlı)
            var settings = SettingsManager.LoadSettings();
            DovizProviderComboBox.SelectedIndex = (int)settings.DovizProvider;
            AltinProviderComboBox.SelectedIndex = (int)settings.AltinProvider;
            HisseProviderComboBox.SelectedIndex = (int)settings.HisseProvider;
            KriptoProviderComboBox.SelectedIndex = (int)settings.KriptoProvider;
            FonProviderComboBox.SelectedIndex = (int)settings.FonProvider;
            DigerProviderComboBox.SelectedIndex = (int)settings.DigerProvider;
            CustomApiTextBox.Text = SettingsManager.GetCustomApiUrl();
            
            // Özel API paneli görünürlüğü
            bool hasCustom = settings.DovizProvider == ApiProviderType.Custom
                || settings.AltinProvider == ApiProviderType.Custom
                || settings.HisseProvider == ApiProviderType.Custom;
            if (hasCustom && CustomApiPanel != null) CustomApiPanel.Visibility = Visibility.Visible;

            // Load Backup Settings
            var backupSettings = SettingsManager.GetBackupSettings();
            if (EnableBackupCheckBox != null) EnableBackupCheckBox.IsChecked = backupSettings.Enabled;
            if (BackupPathTextBox != null) BackupPathTextBox.Text = backupSettings.Directory;
            if (MaxBackupCountTextBox != null) MaxBackupCountTextBox.Text = backupSettings.MaxCount.ToString();
            if (MaxBackupAgeTextBox != null) MaxBackupAgeTextBox.Text = backupSettings.MaxAgeDays.ToString();

            // Load Auto-Lock Settings
            var autoLockSettings = SettingsManager.GetAutoLockSettings();
            if (EnableAutoLockCheckBox != null) EnableAutoLockCheckBox.IsChecked = autoLockSettings.Enabled;
            if (AutoLockTimeoutTextBox != null) AutoLockTimeoutTextBox.Text = autoLockSettings.TimeoutMinutes.ToString();

            // Load Tax Rate Settings
            if (TaxRateHisseTextBox != null) TaxRateHisseTextBox.Text = settings.TaxRateHisseSenedi.ToString();
            if (TaxRateMKYOTextBox != null) TaxRateMKYOTextBox.Text = settings.TaxRateMKYO.ToString();
            if (TaxRateDovizTextBox != null) TaxRateDovizTextBox.Text = settings.TaxRateDöviz.ToString();
            if (TaxRateAltinTextBox != null) TaxRateAltinTextBox.Text = settings.TaxRateAltın.ToString();
            if (TaxRateKriptoTextBox != null) TaxRateKriptoTextBox.Text = settings.TaxRateKripto.ToString();
            if (TaxRateYurtDisiTextBox != null) TaxRateYurtDisiTextBox.Text = settings.TaxRateYurtDışı.ToString();
            if (TaxRateTemettüTextBox != null) TaxRateTemettüTextBox.Text = settings.TaxRateTemettü.ToString();
        }

        // ==================== SECURITY LOGIC ====================

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string current = CurrentPasswordBox.Password;
            string newPwd = NewPasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(current))
            {
                PasswordStatusText.Text = "Lütfen mevcut şifreyi girin.";
                return;
            }

            if (string.IsNullOrWhiteSpace(newPwd) || newPwd.Length < 4)
            {
                PasswordStatusText.Text = "Yeni şifre en az 4 karakter olmalıdır.";
                return;
            }

            if (newPwd != confirm)
            {
                PasswordStatusText.Text = "Yeni şifreler eşleşmiyor.";
                return;
            }

            if (!SettingsManager.VerifyPasswordAndLoadKey(current))
            {
                PasswordStatusText.Text = "Mevcut şifre yanlış.";
                CurrentPasswordBox.Clear();
                CurrentPasswordBox.Focus();
                return;
            }

            if (SettingsManager.ChangePassword(current, newPwd))
            {
                var successWin = new GeneralConfirmWindow("Başarılı", "Şifreniz başarıyla değiştirildi. Artık yeni şifrenizle giriş yapabilirsiniz.", "Tamam", "");
                successWin.Owner = Window.GetWindow(this);
                successWin.ShowDialog();
                
                // Clear boxes
                CurrentPasswordBox.Clear();
                NewPasswordBox.Clear();
                ConfirmPasswordBox.Clear();
                PasswordStatusText.Text = "";
            }
            else
            {
                PasswordStatusText.Text = "Şifre değiştirilemedi (DEK hatası). Lütfen teknik desteğe danışın.";
            }
        }

        // ==================== CLOUD SYNC LOGIC ====================

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Bulut Senkronizasyon Klasörü Seçin",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedFolder = dialog.FolderName;
                string currentPath = SettingsManager.GetDatabasePath();
                string fileName = Path.GetFileName(currentPath);
                string newPath = Path.Combine(selectedFolder, fileName);

                PathTextBox.Text = newPath;
            }
        }

        private void SaveCloudSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            string newPath = PathTextBox.Text;
            string currentPath = SettingsManager.GetDatabasePath();
            
            if (string.IsNullOrWhiteSpace(newPath) || currentPath.Equals(newPath, StringComparison.OrdinalIgnoreCase))
            {
                var infoWin = new GeneralConfirmWindow("Bilgi", "Veritabanı zaten seçili konumda veya yeni bir klasör seçilmedi.", "Tamam", "");
                infoWin.Owner = Window.GetWindow(this);
                infoWin.ShowDialog();
                return;
            }

            try
            {
                var confirmWin = new GeneralConfirmWindow(
                    "Veritabanını Taşı",
                    $"Veritabanı dosyası şu konuma taşınacak:\n\n{newPath}\n\nDevam etmek istiyor musunuz?",
                    "Evet, Taşı",
                    "Vazgeç");
                confirmWin.Owner = Window.GetWindow(this);

                if (confirmWin.ShowDialog() == true)
                {
                    if (File.Exists(newPath))
                    {
                        var conflictWin = new GeneralConfirmWindow(
                            "Dosya Çakışması",
                            "Hedef klasörde zaten bir fintrack.db dosyası var. Mevcut dosyayı SEÇİLEN klasördeki ile değiştirmek ister misiniz?\n\n(Eski yerel verileriniz korunacaktır ancak aktif dosya buluttaki olacaktır)",
                            "Değiştir",
                            "Vazgeç");
                        conflictWin.Owner = Window.GetWindow(this);
                        conflictWin.SetHighContrast(true);
                        
                        if (conflictWin.ShowDialog() != true) return;
                    }
                    else
                    {
                        File.Copy(currentPath, newPath, true);
                    }

                    SettingsManager.SetDatabasePath(newPath);

                    var successWin = new GeneralConfirmWindow("Başarılı", "Veritabanı konumu başarıyla güncellendi!\n\nDeğişikliklerin tam olarak uygulanması için lütfen uygulamayı kapatıp tekrar açın.", "Tamam", "");
                    successWin.Owner = Window.GetWindow(this);
                    successWin.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                var errorWin = new GeneralConfirmWindow("Hata", $"Dosya taşıma sırasında bir hata oluştu: {ex.Message}", "Tamam", "");
                errorWin.Owner = Window.GetWindow(this);
                errorWin.SetHighContrast(true);
                errorWin.ShowDialog();
            }
        }

        // ==================== AUTO BACKUP LOGIC ====================

        private void SelectBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Otomatik Yedekleme Klasörü Seçin",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                BackupPathTextBox.Text = dialog.FolderName;
            }
        }

        private void SaveBackupSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            bool isEnabled = EnableBackupCheckBox.IsChecked ?? false;
            string backupDir = BackupPathTextBox.Text;
            
            if (isEnabled && string.IsNullOrWhiteSpace(backupDir))
            {
                var dialog = new GeneralConfirmWindow("Eksik Bilgi", "Otomatik yedekleme açıkken lütfen bir yedekleme klasörü seçin.", "Tamam", "");
                dialog.ShowDialog();
                return;
            }

            if (!int.TryParse(MaxBackupCountTextBox.Text, out int maxCount) || maxCount < 1)
            {
                var dialog = new GeneralConfirmWindow("Geçersiz Veri", "Maksimum yedek sayısı geçerli bir sayı olmalı ve 1'den büyük olmalıdır.", "Tamam", "");
                dialog.ShowDialog();
                return;
            }

            if (!int.TryParse(MaxBackupAgeTextBox.Text, out int maxAgeDays) || maxAgeDays < 1)
            {
                var dialog = new GeneralConfirmWindow("Geçersiz Veri", "Maksimum saklama günü geçerli bir sayı olmalı ve 1'den büyük olmalıdır.", "Tamam", "");
                dialog.ShowDialog();
                return;
            }

            SettingsManager.SaveBackupSettings(isEnabled, backupDir, maxCount, maxAgeDays);
            
            var successDialog = new GeneralConfirmWindow("Başarılı", "Yedekleme ayarları başarıyla kaydedildi.", "Tamam", "");
            successDialog.ShowDialog();
        }

        // ==================== AUTO LOCK LOGIC ====================

        private void SaveAutoLockSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            bool isEnabled = EnableAutoLockCheckBox.IsChecked ?? false;

            if (!int.TryParse(AutoLockTimeoutTextBox.Text, out int timeoutMin) || timeoutMin < 1)
            {
                var dialog = new GeneralConfirmWindow("Geçersiz Veri", "Kilitlenme süresi (dakika) geçerli bir sayı olmalı ve 1'den büyük olmalıdır.", "Tamam", "");
                dialog.ShowDialog();
                return;
            }

            SettingsManager.SaveAutoLockSettings(isEnabled, timeoutMin);
            
            // Reconfigure the running service immediately so changes take effect
            FinTrack.WPF.Services.AutoLockService.Reconfigure();
            
            var successDialog = new GeneralConfirmWindow("Başarılı", "Otomatik kilitleme ayarları kaydedildi.", "Tamam", "");
            successDialog.ShowDialog();
        }

        // ==================== API PROVIDER LOGIC ====================

        private void SaveApiSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var doviz = (ApiProviderType)DovizProviderComboBox.SelectedIndex;
            var altin = (ApiProviderType)AltinProviderComboBox.SelectedIndex;
            var hisse = (ApiProviderType)HisseProviderComboBox.SelectedIndex;
            var kripto = (ApiProviderType)KriptoProviderComboBox.SelectedIndex;
            var fon = (ApiProviderType)FonProviderComboBox.SelectedIndex;
            var diger = (ApiProviderType)DigerProviderComboBox.SelectedIndex;

            SettingsManager.SaveCategoryProviders(doviz, altin, hisse, kripto, fon, diger);

            // Özel API URL kaydet
            bool hasCustom = doviz == ApiProviderType.Custom || altin == ApiProviderType.Custom 
                || hisse == ApiProviderType.Custom || kripto == ApiProviderType.Custom;
            if (hasCustom)
            {
                SettingsManager.SetCustomApiUrl(CustomApiTextBox.Text);
            }

            // Özel API paneli görünürlüğünü güncelle
            if (CustomApiPanel != null)
            {
                CustomApiPanel.Visibility = hasCustom ? Visibility.Visible : Visibility.Collapsed;
            }

            var successWin = new GeneralConfirmWindow("Başarılı", "API ayarları başarıyla kaydedildi.\n\nYatırımlar sayfasında 'Fiyatları Çek' butonuyla yeni ayarları test edebilirsiniz.", "Tamam", "");
            successWin.Owner = Window.GetWindow(this);
            successWin.ShowDialog();
        }




        private void NewCategoryTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Filter the ParentCategoryComboBox based on selected Type here
            // Currently loading all top-level categories, so it might be better to filter them by type dynamically.
            if (ParentCategoryComboBox != null && ParentCategoryComboBox.ItemsSource != null)
            {
                var typeIndex = NewCategoryTypeComboBox.SelectedIndex;
                FinTrack.Core.Models.TransactionType selectedType = FinTrack.Core.Models.TransactionType.Expense;
                if (typeIndex == 1) selectedType = FinTrack.Core.Models.TransactionType.Income;

                using var context = GetDbContext();

                var parents = context.Categories.Where(c => c.IsVisible && c.ParentCategoryId == null && c.Type == selectedType)
                                                .OrderBy(c => c.Name).ToList();
                parents.Insert(0, new FinTrack.Core.Models.Category { Id = 0, Name = "-- Yok (Ana Kategori) --" });
                
                var previousSelection = ParentCategoryComboBox.SelectedItem as FinTrack.Core.Models.Category;
                
                ParentCategoryComboBox.ItemsSource = parents;
                
                if (previousSelection != null && parents.Any(p => p.Id == previousSelection.Id))
                    ParentCategoryComboBox.SelectedItem = parents.First(p => p.Id == previousSelection.Id);
                else
                    ParentCategoryComboBox.SelectedIndex = 0;
            }
        }

        // ==================== CATEGORY MANAGEMENT LOGIC ====================

        private FinTrack.Data.AppDbContext GetDbContext()
        {
            var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<FinTrack.Data.AppDbContext>();
            string dbPath = FinTrack.Core.Services.SettingsManager.GetDatabasePath();
            string? password = FinTrack.Core.Services.SettingsManager.ActiveDataKey;

            if (string.IsNullOrEmpty(password))
            {
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
            else
            {
                optionsBuilder.UseSqlite($"Data Source={dbPath};Password={password}");
            }

            return new FinTrack.Data.AppDbContext(optionsBuilder.Options);
        }

        private void LoadCategories()
        {
            try
            {
                using var context = GetDbContext();
                // Filter out Transfers from management lists - they are handled differently
                var categories = context.Categories
                    .Include(c => c.ParentCategory)
                    .Where(c => c.Type != FinTrack.Core.Models.TransactionType.Transfer)
                    .ToList();

                VisibleCategoriesListBox.ItemsSource = SortCategoriesHierarchically(categories.Where(c => c.IsVisible).ToList());
                HiddenCategoriesListBox.ItemsSource = SortCategoriesHierarchically(categories.Where(c => !c.IsVisible).ToList());

                // Update Parent Category ComboBox (Initial Load for default Gider/Expense type)
                var visibleParents = categories.Where(c => c.IsVisible && c.ParentCategoryId == null && c.Type == FinTrack.Core.Models.TransactionType.Expense).OrderBy(c => c.Name).ToList();
                visibleParents.Insert(0, new FinTrack.Core.Models.Category { Id = 0, Name = "-- Yok (Ana Kategori) --" }); 
                ParentCategoryComboBox.ItemsSource = visibleParents;
                ParentCategoryComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                 var errorWin = new GeneralConfirmWindow("Hata", $"Kategoriler yüklenirken hata oluştu: {ex.Message}", "Tamam", "");
                 errorWin.Owner = Window.GetWindow(this);
                 errorWin.SetHighContrast(true);
                 errorWin.ShowDialog();
            }
        }

        private System.Collections.Generic.List<FinTrack.Core.Models.Category> SortCategoriesHierarchically(System.Collections.Generic.List<FinTrack.Core.Models.Category> flatList)
        {
            var result = new System.Collections.Generic.List<FinTrack.Core.Models.Category>();
            
            // Group by type first (Income vs Expense)
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

                // Add any sub-categories whose parent is not in the current list/filter
                var orphans = typeCategories.Where(c => c.ParentCategoryId != null && !parents.Any(p => p.Id == c.ParentCategoryId)).OrderBy(c => c.Name).ToList();
                result.AddRange(orphans);
            }
            
            return result;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
             LoadCategories();
        }

        private void HideCategory_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = new System.Collections.Generic.List<FinTrack.Core.Models.Category>();
            foreach (var item in VisibleCategoriesListBox.SelectedItems)
            {
                selectedItems.Add( (FinTrack.Core.Models.Category)item );
            }

            if (selectedItems.Count == 0) return;

            try
            {
                using var context = GetDbContext();
                foreach (var cat in selectedItems)
                {
                    var dbCat = context.Categories.Find(cat.Id);
                    if (dbCat != null)
                    {
                        dbCat.IsVisible = false;
                    }
                }
                context.SaveChanges();
                LoadCategories();
            }
            catch (Exception ex)
            {
                var errorWin = new GeneralConfirmWindow("Hata", $"Kategori gizlenirken hata oluştu: {ex.Message}", "Tamam", "");
                errorWin.ShowDialog();
            }
        }

        private void ShowCategory_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = new System.Collections.Generic.List<FinTrack.Core.Models.Category>();
            foreach (var item in HiddenCategoriesListBox.SelectedItems)
            {
                selectedItems.Add( (FinTrack.Core.Models.Category)item );
            }

            if (selectedItems.Count == 0) return;

            try
            {
                using var context = GetDbContext();
                foreach (var cat in selectedItems)
                {
                    var dbCat = context.Categories.Find(cat.Id);
                    if (dbCat != null)
                    {
                        dbCat.IsVisible = true;
                    }
                }
                context.SaveChanges();
                LoadCategories();
            }
            catch (Exception ex)
            {
                var errorWin = new GeneralConfirmWindow("Hata", $"Kategori gösterilirken hata oluştu: {ex.Message}", "Tamam", "");
                errorWin.ShowDialog();
            }
        }

        private void AddNewCategory_Click(object sender, RoutedEventArgs e)
        {
            string categoryName = NewCategoryNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                var warnWin = new GeneralConfirmWindow("Uyarı", "Lütfen bir kategori adı giriniz.", "Tamam", "");
                warnWin.ShowDialog();
                return;
            }

            // Determine Type
            FinTrack.Core.Models.TransactionType catType = FinTrack.Core.Models.TransactionType.Expense;
            if (NewCategoryTypeComboBox.SelectedIndex == 1) catType = FinTrack.Core.Models.TransactionType.Income;

            try
            {
                using var context = GetDbContext();
                
                int? parentId = null;
                if (ParentCategoryComboBox.SelectedItem is FinTrack.Core.Models.Category selectedParent && selectedParent.Id > 0)
                {
                    parentId = selectedParent.Id;
                }

                var newCat = new FinTrack.Core.Models.Category
                {
                    Name = categoryName,
                    Type = catType,
                    IsVisible = true,
                    ParentCategoryId = parentId
                };

                context.Categories.Add(newCat);
                context.SaveChanges();

                var successWin = new GeneralConfirmWindow("Başarılı", $"'{categoryName}' kategorisi başarıyla eklendi.", "Tamam", "");
                successWin.ShowDialog();

                NewCategoryNameTextBox.Clear();
                LoadCategories();
            }
            catch (Exception ex)
            {
                var errorWin = new GeneralConfirmWindow("Hata", $"Kategori eklenirken hata oluştu: {ex.Message}", "Tamam", "");
                errorWin.ShowDialog();
            }
        }
        // ==================== TAX SETTINGS LOGIC ====================

        private void SaveTaxSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                decimal ParseTaxRate(System.Windows.Controls.TextBox tb)
                {
                    if (decimal.TryParse(tb.Text.Replace(",", "."),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal val))
                        return val;
                    return 0;
                }

                SettingsManager.SaveTaxSettings(
                    hisse: ParseTaxRate(TaxRateHisseTextBox),
                    mkyo: ParseTaxRate(TaxRateMKYOTextBox),
                    temettü: ParseTaxRate(TaxRateTemettüTextBox),
                    döviz: ParseTaxRate(TaxRateDovizTextBox),
                    altın: ParseTaxRate(TaxRateAltinTextBox),
                    kripto: ParseTaxRate(TaxRateKriptoTextBox),
                    yurtDışı: ParseTaxRate(TaxRateYurtDisiTextBox)
                );

                var successWin = new GeneralConfirmWindow("Başarılı", "Vergi oranları başarıyla kaydedildi.", "Tamam", "");
                successWin.Owner = Window.GetWindow(this);
                successWin.ShowDialog();
            }
            catch (Exception ex)
            {
                var errorWin = new GeneralConfirmWindow("Hata", $"Vergi ayarları kaydedilirken hata oluştu: {ex.Message}", "Tamam", "");
                errorWin.Owner = Window.GetWindow(this);
                errorWin.ShowDialog();
            }
        }

        private void CategoryEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is ListBoxItem listBoxItem)
            {
                var category = listBoxItem.Content as FinTrack.Core.Models.Category;
                if (category != null)
                {
                    var editWin = new CategoryEditWindow(category.Id);
                    editWin.Owner = Window.GetWindow(this);
                    if (editWin.ShowDialog() == true)
                    {
                        LoadCategories();
                    }
                }
            }
        }
    }
}
