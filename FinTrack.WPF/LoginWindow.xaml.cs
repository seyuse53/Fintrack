using System.Windows;
using FinTrack.Core.Services;

namespace FinTrack.WPF
{
    public partial class LoginWindow : Window
    {
        private bool _isFirstLaunch;

        public LoginWindow()
        {
            InitializeComponent();
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            var profiles = SettingsManager.GetProfiles();
            
            // EĞER HİÇ PROFİL YOKSA (İLK AÇILIŞ)
            if (!profiles.Any())
            {
                var addProfileWin = new AddProfileWindow();
                if (addProfileWin.ShowDialog() == true)
                {
                    SettingsManager.CreateProfile(addProfileWin.ProfileName, addProfileWin.SelectedDbPath);
                    profiles = SettingsManager.GetProfiles();
                }
                else
                {
                    // If they cancel initial profile creation, we must close
                    Application.Current.Shutdown();
                    return;
                }
            }

            ProfileComboBox.ItemsSource = profiles;
            ProfileComboBox.SelectedItem = profiles.FirstOrDefault();
            
            CheckFirstLaunch();
        }

        private void ProfileComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is string profile)
            {
                SettingsManager.SwitchProfile(profile);
                CheckFirstLaunch();
            }
        }

        private void RemoveProfileFromList_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is string profile)
            {
                var confirmWin = new GeneralConfirmWindow(
                    "Profil Bağlantısını Kes", 
                    $"'{profile}' profilini bu bilgisayardan ayırmak istediğinize emin misiniz?\n\nVeritabanı dosyanız SİLİNMEYECEK, sadece bu listeden kaldırılacaktır. Başka bir bilgisayarda tekrar açabilirsiniz.",
                    "Bağlantıyı Kes",
                    "Vazgeç");
                confirmWin.Owner = this;
                
                if (confirmWin.ShowDialog() == true)
                {
                    // deleteDatabase=false: Sadece profil listesinden ve settings dosyasından siler, DB kalır.
                    SettingsManager.DeleteProfile(profile, false);
                    
                    var infoWin = new GeneralConfirmWindow("Bağlantı Kesildi", $"'{profile}' profilinin bu bilgisayar ile bağlantısı kesildi. Veritabanı dosyanız güvende.", "Tamam", "");
                    infoWin.Owner = this;
                    infoWin.ShowDialog();
                    
                    LoadProfiles();
                }
            }
        }

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            var addProfileWin = new AddProfileWindow();
            if (addProfileWin.ShowDialog() == true)
            {
                SettingsManager.CreateProfile(addProfileWin.ProfileName, addProfileWin.SelectedDbPath);
                LoadProfiles();
                ProfileComboBox.SelectedItem = addProfileWin.ProfileName;
            }
        }

        private void DeleteProfileAuth_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is string profile)
            {
                // STEP 1: FIRST CONFIRMATION (MODERN)
                var firstConfirmWin = new GeneralConfirmWindow(
                    "DİKKAT: Profil Siliniyor", 
                    $"'{profile}' profilini ve TÜM ayarlarını kalıcı olarak silmek istediğinize emin misiniz?\nBu işlem geri alınamaz.",
                    "Evet, Devam Et",
                    "Vazgeç");
                firstConfirmWin.Owner = this;
                firstConfirmWin.SetHighContrast(true); // Red button for dangerous action

                if (firstConfirmWin.ShowDialog() != true) return;
                
                // STEP 2: SHOW PASSWORD WINDOW
                var confirmWin = new ConfirmPasswordWindow();
                confirmWin.Owner = this;
                
                if (confirmWin.ShowDialog() == true)
                {
                    // STEP 3: VERIFY PASSWORD
                    if (SettingsManager.VerifyPasswordAndLoadKey(confirmWin.Password))
                    {
                        // STEP 4: SHOW MODERN DELETE DATA CONFIRMATION WINDOW
                        var deleteDataConfirmWin = new DeleteDataConfirmWindow(profile);
                        deleteDataConfirmWin.Owner = this;
                        
                        if (deleteDataConfirmWin.ShowDialog() == true)
                        {
                            if (deleteDataConfirmWin.Result != MessageBoxResult.Cancel)
                            {
                                bool deleteDb = (deleteDataConfirmWin.Result == MessageBoxResult.Yes);
                                SettingsManager.DeleteProfile(profile, deleteDb); 
                                
                                var successWin = new GeneralConfirmWindow("Sistem Bilgisi", $"'{profile}' profili başarıyla silindi.", "Tamam", "");
                                successWin.Owner = this;
                                successWin.ShowDialog();
                                
                                LoadProfiles();
                            }
                        }
                    }
                    else
                    {
                        var errorWin = new GeneralConfirmWindow("Hata", "Yanlış şifre girdiniz. Silme işlemi iptal edildi.", "Tamam", "");
                        errorWin.Owner = this;
                        errorWin.SetHighContrast(true);
                        errorWin.ShowDialog();
                    }
                }
            }
        }

        private void CheckFirstLaunch()
        {
            _isFirstLaunch = !SettingsManager.IsPasswordSet();

            if (_isFirstLaunch)
            {
                InstructionText.Text = "Hemen Yeni Bir Şifre Oluşturun";
                ConfirmButton.Content = "Şifreyi Kaydet ve Başla";
                ConfirmPasswordLabel.Visibility = Visibility.Visible;
                PasswordBoxConfirm.Visibility = Visibility.Visible;
            }
            else
            {
                InstructionText.Text = "FinTrack'e Giriş Yapın";
                ConfirmButton.Content = "Giriş";
                ConfirmPasswordLabel.Visibility = Visibility.Collapsed;
                PasswordBoxConfirm.Visibility = Visibility.Collapsed;
            }
            
            // Profil değişiminde şifre alanını temizle
            PasswordBoxInput.Clear();
            PasswordBoxConfirm.Clear();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            var password = PasswordBoxInput.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                var emptyWin = new GeneralConfirmWindow("Hata", "Şifre alanı boş bırakılamaz!", "Tamam", "");
                emptyWin.Owner = this;
                emptyWin.SetHighContrast(true);
                emptyWin.ShowDialog();
                return;
            }

            if (_isFirstLaunch)
            {
                var confirmPassword = PasswordBoxConfirm.Password;
                if (password != confirmPassword)
                {
                    var noMatchWin = new GeneralConfirmWindow("Doğrulama Hatası", "Girdiğiniz şifreler eşleşmiyor. Lütfen kontrol edip tekrar deneyin.", "Tamam", "");
                    noMatchWin.Owner = this;
                    noMatchWin.SetHighContrast(true);
                    noMatchWin.ShowDialog();
                    return;
                }

                string recoveryCode = SettingsManager.SetupFirstTimePassword(password);
                
                // Show Recovery Code to user
                var recoveryWindow = new RecoveryCodeWindow(recoveryCode);
                recoveryWindow.ShowDialog();

                DialogResult = true;
                Close();
            }
            else
            {
                if (SettingsManager.VerifyPasswordAndLoadKey(password))
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    var accessDeniedWin = new GeneralConfirmWindow("Erişim Reddedildi", "Hatalı şifre girdiniz, lütfen tekrar deneyin.", "Tamam", "");
                    accessDeniedWin.Owner = this;
                    accessDeniedWin.SetHighContrast(true);
                    accessDeniedWin.ShowDialog();
                    
                    PasswordBoxInput.Clear();
                }
            }
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_isFirstLaunch)
            {
                    var infoWin = new GeneralConfirmWindow("Bilgi", "Henüz bir şifre oluşturmadınız.", "Tamam", "");
                    infoWin.Owner = this;
                    infoWin.ShowDialog();
                return;
            }

            var resetWindow = new PasswordResetWindow();
            if (resetWindow.ShowDialog() == true)
            {
                // Password was successfully reset and DEK is in memory. We can proceed!
                DialogResult = true;
                Close();
            }
        }
    }
}
