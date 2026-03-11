using System.Windows;
using FinTrack.Core.Services;

namespace FinTrack.WPF
{
    public partial class PasswordResetWindow : Window
    {
        public PasswordResetWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            string code = RecoveryCodeInput.Text.Trim();
            string pass1 = NewPasswordInput.Password;
            string pass2 = NewPasswordConfirmInput.Password;

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(pass1))
            {
                MessageBox.Show("Lütfen kurtarma kodunuzu ve yeni şifrenizi girin.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pass1 != pass2)
            {
                MessageBox.Show("Girdiğiniz yeni şifreler eşleşmiyor.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool success = SettingsManager.ResetPasswordWithRecoveryCode(code, pass1);

            if (success)
            {
                var infoDialog = new FinTrack.WPF.Views.InfoDialogWindow("Başarılı", "Şifreniz başarıyla sıfırlandı ve verileriniz kurtarıldı. Uygulamaya giriş yapılıyor.");
                infoDialog.Owner = this;
                infoDialog.ShowDialog();
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Girdiğiniz kurtarma kodu geçersiz veya hatalı.", "Erişim Reddedildi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
