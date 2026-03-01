using System.Windows;

namespace FinTrack.WPF
{
    public partial class DeleteDataConfirmWindow : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        public DeleteDataConfirmWindow(string profileName)
        {
            InitializeComponent();
            MessageText.Text = $"'{profileName}' profiline ait veritabanı dosyası (harcamalar, hesaplar vb.) da silinsin mi?";
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            DialogResult = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}
