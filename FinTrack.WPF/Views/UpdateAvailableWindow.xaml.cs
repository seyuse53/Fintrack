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
            // UI Güncelleme: İndirme başlıyor
            UpdateNowButton.IsEnabled = false;
            RemindLaterButton.IsEnabled = false;
            ReleaseNotesBorder.Visibility = Visibility.Collapsed; // Sürüm notlarını gizle
            DownloadProgressText.Visibility = Visibility.Visible;
            DownloadProgressBar.Visibility = Visibility.Visible;
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

            // 1. Download the new executable with progress reporting
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("FinTrackUpdater", "1.0"));
                
                var response = await client.GetAsync(_releaseInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength;

                using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int read;

                    DownloadProgressBar.IsIndeterminate = false;
                    DownloadProgressBar.Minimum = 0;
                    DownloadProgressBar.Maximum = totalBytes ?? 100;

                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (totalBytes.HasValue)
                        {
                            double progress = (double)totalRead / totalBytes.Value * 100;
                            Dispatcher.Invoke(() =>
                            {
                                DownloadProgressBar.Value = totalRead;
                                DownloadProgressText.Text = $"İndiriliyor: %{progress:F0} ({(totalRead / 1024.0 / 1024.0):F1} MB / {(totalBytes.Value / 1024.0 / 1024.0):F1} MB)";
                            });
                        }
                    }
                }
            }

            Dispatcher.Invoke(() => DownloadProgressText.Text = "Uygulama güncelleniyor, lütfen bekleyin...");

            // 2. Create a more robust updater script
            string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? "FinTrack.exe";
            string scriptPath = Path.Combine(Path.GetTempPath(), "FinTrackUpdater.bat");
            
            // This script waits effectively by looping until the delete is successful
            string scriptContent = $@"
@echo off
set ""EXE_PATH={currentExePath}""
set ""TEMP_PATH={tempFilePath}""

:loop
timeout /t 1 /nobreak > nul
del /f /q ""%EXE_PATH%"" 2>nul
if exist ""%EXE_PATH%"" goto loop

move /y ""%TEMP_PATH%"" ""%EXE_PATH%""
start """" ""%EXE_PATH%""
del ""%~f0""
";
            File.WriteAllText(scriptPath, scriptContent);

            // 3. Launch the script
            ProcessStartInfo procInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(procInfo);

            // 4. Shutdown
            Application.Current.Shutdown();
        }
    }
}
