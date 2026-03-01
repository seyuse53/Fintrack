using System.Windows;

namespace FinTrack.WPF
{
    public partial class ConfirmPasswordWindow : Window
    {
        public string Password { get; private set; } = string.Empty;

        public ConfirmPasswordWindow()
        {
            InitializeComponent();
            PasswordInput.Focus();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordInput.Password))
            {
                MessageBox.Show("Lütfen şifrenizi girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Password = PasswordInput.Password;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
