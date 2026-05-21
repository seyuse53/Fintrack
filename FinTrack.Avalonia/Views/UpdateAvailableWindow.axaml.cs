using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FinTrack.Core.Services;

namespace FinTrack.Avalonia.Views;

public partial class UpdateAvailableWindow : Window
{
    private GitHubReleaseInfo? _releaseInfo;

    public UpdateAvailableWindow()
    {
        InitializeComponent();
    }

    public void LoadData(string currentVersion, GitHubReleaseInfo releaseInfo)
    {
        _releaseInfo = releaseInfo;
        
        var currentText = this.FindControl<TextBlock>("CurrentVersionText");
        if (currentText != null) currentText.Text = $"Mevcut Sürüm: v{currentVersion}";
        
        var newText = this.FindControl<TextBlock>("NewVersionText");
        if (newText != null) newText.Text = $"Yeni Sürüm: v{releaseInfo.Version}";
        
        var notesText = this.FindControl<TextBlock>("ReleaseNotesText");
        if (notesText != null) notesText.Text = releaseInfo.ReleaseNotes;
    }

    private void RemindLaterButton_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void UpdateNowButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_releaseInfo != null && !string.IsNullOrEmpty(_releaseInfo.DownloadUrl))
        {
            // Try to open the exact file URL, otherwise fallback to release page
            OpenUrl(_releaseInfo.DownloadUrl);
        }
        else
        {
            OpenUrl("https://github.com/seyuse53/Fintrack/releases/latest");
        }
        this.Close();
    }

    private void OpenUrl(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch
        {
            // Silently ignore failures if we can't open the browser
        }
    }
}
