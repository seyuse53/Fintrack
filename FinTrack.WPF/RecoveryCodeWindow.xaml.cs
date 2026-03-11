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
            var infoDialog = new FinTrack.WPF.Views.InfoDialogWindow("Bilgi", "Kurtarma kodu panoya kopyalandı.");
            infoDialog.Owner = this;
            infoDialog.ShowDialog();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
