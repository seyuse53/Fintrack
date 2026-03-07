using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Core.Services;
using Microsoft.Win32;

namespace FinTrack.WPF.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            PathTextBox.Text = SettingsManager.GetDatabasePath();
            
            // Load API Provider
            var currentApi = SettingsManager.GetApiProvider();
            ApiProviderComboBox.SelectedIndex = (int)currentApi;
            CustomApiTextBox.Text = SettingsManager.GetCustomApiUrl();
            
            if (currentApi == ApiProviderType.Custom)
            {
                if (CustomApiPanel != null) CustomApiPanel.Visibility = Visibility.Visible;
            }

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

        private void ApiProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ApiProviderComboBox.SelectedIndex >= 0 && CustomApiPanel != null)
            {
                var selectedProvider = (ApiProviderType)ApiProviderComboBox.SelectedIndex;
                if (selectedProvider == ApiProviderType.Custom)
                {
                    CustomApiPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    CustomApiPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SaveApiSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApiProviderComboBox.SelectedIndex >= 0)
            {
                var selectedProvider = (ApiProviderType)ApiProviderComboBox.SelectedIndex;
                SettingsManager.SetApiProvider(selectedProvider);

                if (selectedProvider == ApiProviderType.Custom)
                {
                    SettingsManager.SetCustomApiUrl(CustomApiTextBox.Text);
                }

                var successWin = new GeneralConfirmWindow("Başarılı", "Fiyat sağlayıcı (API) ayarları başarıyla kaydedildi.", "Tamam", "");
                successWin.Owner = Window.GetWindow(this);
                successWin.ShowDialog();
            }
        }
    }
}
