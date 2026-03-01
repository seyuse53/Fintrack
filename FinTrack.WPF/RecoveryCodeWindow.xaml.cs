using System.Windows;

namespace FinTrack.WPF
{
    public partial class RecoveryCodeWindow : Window
    {
        public RecoveryCodeWindow(string recoveryCode)
        {
            InitializeComponent();
            RecoveryCodeTextBox.Text = recoveryCode;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(RecoveryCodeTextBox.Text);
            MessageBox.Show("Kurtarma kodu panoya kopyalandı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
