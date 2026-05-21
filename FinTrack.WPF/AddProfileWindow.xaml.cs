using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using FinTrack.WPF.Views;

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

                // Auto-fill ProfileName if it's empty
                if (string.IsNullOrWhiteSpace(ProfileNameInput.Text) || ProfileNameInput.Text == "[Profilİsmi]")
                {
                    string fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    if (fileName.StartsWith("fintrack_", System.StringComparison.OrdinalIgnoreCase))
                    {
                        fileName = fileName.Substring(9); // remove "fintrack_" prefix
                    }
                    ProfileNameInput.Text = fileName;
                }
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProfileNameInput.Text))
            {
                var errorDialog = new InfoDialogWindow(
                    "Profil İsmi Gerekli",
                    "Lütfen bir profil ismi girin.",
                    "Tamam"
                );
                errorDialog.Owner = this;
                errorDialog.ShowDialog();
                return;
            }

            ProfileName = ProfileNameInput.Text.Trim();

            // Check if profile already exists
            var existingProfiles = FinTrack.Core.Services.SettingsManager.GetProfiles();
            if (existingProfiles.Contains(ProfileName, StringComparer.OrdinalIgnoreCase))
            {
                var errorDialog = new InfoDialogWindow(
                    "Profil Zaten Mevcut",
                    $"'{ProfileName}' isminde bir profil zaten mevcut. Lütfen farklı bir profil ismi belirleyin.",
                    "Tamam"
                );
                errorDialog.Owner = this;
                errorDialog.ShowDialog();
                return;
            }

            // Quick check for existing keys before we close
            if (!string.IsNullOrEmpty(SelectedDbPath))
            {
                string keysFile = SelectedDbPath + ".keys";
                if (File.Exists(keysFile))
                {
                    var infoDialog = new InfoDialogWindow(
                        "Veriler İçe Aktarıldı",
                        "Seçilen veritabanı için şifreleme anahtarları bulundu!\n\nYeni bir şifre belirlemenize gerek kalmayacak. Giriş ekranında mevcut ana şifrenizi kullanarak devam edebilirsiniz.",
                        "Tamam"
                    );
                    infoDialog.Owner = this;
                    infoDialog.ShowDialog();
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
