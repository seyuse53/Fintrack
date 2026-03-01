using System.Windows;
using System.Windows.Input;
using FinTrack.Core.Services;

namespace FinTrack.WPF.Views
{
    public partial class LockScreenWindow : Window
    {
        public LockScreenWindow()
        {
            InitializeComponent();
            PasswordInput.Focus();
        }

        private void Unlock_Click(object sender, RoutedEventArgs e)
        {
            VerifyAndUnlock();
        }

        private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VerifyAndUnlock();
            }
        }

        private void VerifyAndUnlock()
        {
            string password = PasswordInput.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorText.Text = "Lütfen uygulama şifrenizi girin.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            // Doğrulama SettingsManager üzerinden yapılıyor
            // VerifyPasswordAndLoadKey metodu şifreyi kontrol eder, doğruysa ActiveDataKey'i tekrar atar
            if (SettingsManager.VerifyPasswordAndLoadKey(password))
            {
                DialogResult = true;
                Services.AutoLockService.MarkUnlocked();
                Close();
            }
            else
            {
                ErrorText.Text = "Hatalı şifre girdiniz.";
                ErrorText.Visibility = Visibility.Visible;
                PasswordInput.Clear();
                PasswordInput.Focus();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Kullanıcı kilidi açmak yerine tamamen çıkmak istiyorsa
            Application.Current.Shutdown();
        }
    }
}
