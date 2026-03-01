using System.Windows;
using Microsoft.Win32;
using System.IO;

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

            // Quick check for existing keys before we close
            if (!string.IsNullOrEmpty(SelectedDbPath))
            {
                string keysFile = SelectedDbPath + ".keys";
                if (File.Exists(keysFile))
                {
                    MessageBox.Show("Seçilen veritabanı için şifreleme anahtarları bulundu!\n\nYeni bir şifre belirlemenize gerek kalmayacak. Giriş ekranında mevcut ana şifrenizi kullanarak devam edebilirsiniz.", 
                        "Veriler İçe Aktarıldı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

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
