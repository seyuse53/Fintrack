using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using FinTrack.Core.Services;

namespace FinTrack.WPF.Views
{
    public partial class UpdateAvailableWindow : Window
    {
        private readonly GitHubReleaseInfo _releaseInfo;
        private readonly string _currentVersion;

        public UpdateAvailableWindow(string currentVersion, GitHubReleaseInfo releaseInfo)
        {
            InitializeComponent();
            _currentVersion = currentVersion;
            _releaseInfo = releaseInfo;

            LoadData();
        }

        private void LoadData()
        {
            CurrentVersionText.Text = $"Mevcut Sürüm: v{_currentVersion}";
            NewVersionText.Text = $"Yeni Sürüm: v{_releaseInfo.Version}";
            ReleaseNotesText.Text = _releaseInfo.ReleaseNotes;
        }

        private void RemindLaterButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void UpdateNowButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable buttons
            UpdateNowButton.IsEnabled = false;
            RemindLaterButton.IsEnabled = false;
            
            // Show progress
            DownloadProgressBar.Visibility = Visibility.Visible;
            DownloadProgressText.Visibility = Visibility.Visible;
            DownloadProgressBar.IsIndeterminate = true;
            DownloadProgressText.Text = "Güncelleme indiriliyor, Lütfen bekleyin...";

            try
            {
                await DownloadAndApplyUpdateAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Güncelleme indirilirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private async Task DownloadAndApplyUpdateAsync()
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), "FinTrackUpdate.exe");

            // 1. Download the new executable
            using (var client = new HttpClient())
            {
                // GitHub releases sometimes redirect, so HttpClient handles it automatically
                var response = await client.GetAsync(_releaseInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }

            // 2. Create the updater script
            string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? "FinTrack.exe";
            string scriptPath = Path.Combine(Path.GetTempPath(), "FinTrackUpdater.bat");
            
            // The batch script does the following:
            // - waits 3 seconds to let the current app close completely
            // - deletes the old exe
            // - moves the new exe to the current location
            // - starts the new exe
            // - deletes itself
            string scriptContent = $@"
@echo off
timeout /t 3 /nobreak > NUL
del /f /q ""{currentExePath}""
move /y ""{tempFilePath}"" ""{currentExePath}""
start """" ""{currentExePath}""
del ""%~f0""
";
            File.WriteAllText(scriptPath, scriptContent);

            // 3. Launch the script hidden
            ProcessStartInfo procInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(procInfo);

            // 4. Close the application gracefully to let the script replace it
            Application.Current.Shutdown();
        }
    }
}
