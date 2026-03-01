using System.Windows;
using Microsoft.Win32;

namespace FinTrack.WPF
{
    public partial class AddProfileWindow : Window
    {
        public string ProfileName { get; private set; } = string.Empty;
        public string? SelectedDbPath { get; private set; }

        public AddProfileWindow()
        {
            InitializeComponent();
            ProfileNameInput.TextChanged += (s, e) =>
            {
                if (string.IsNullOrEmpty(DbPathInput.Text) || DbPathInput.Text.Contains("Documents\\FinTrack"))
                {
                    string pName = string.IsNullOrWhiteSpace(ProfileNameInput.Text) ? "[Profilİsmi]" : ProfileNameInput.Text;
                    DbPathInput.Text = FinTrack.Core.Services.SettingsManager.GetDefaultDbPath(pName);
                }
            };
        }

        private void BrowseDb_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                Title = "Önceden Mevcut Bir Veritabanı Seçin"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                DbPathInput.Text = openFileDialog.FileName;
                SelectedDbPath = openFileDialog.FileName;
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProfileNameInput.Text))
            {
                MessageBox.Show("Lütfen bir profil ismi girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProfileName = ProfileNameInput.Text.Trim();
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
