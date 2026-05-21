using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace FinTrack.Avalonia.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string title, string message, string confirmText = "Tamam", string cancelText = "")
        : this()
    {
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;

        // Set icon based on title keywords
        if (title.Contains("Hata") || title.Contains("Error"))
            IconText.Text = "❌";
        else if (title.Contains("Uyarı") || title.Contains("Dikkat") || title.Contains("Warning"))
            IconText.Text = "⚠️";
        else if (title.Contains("Başarılı") || title.Contains("Success"))
            IconText.Text = "✅";
        else
            IconText.Text = "ℹ️";

        if (string.IsNullOrEmpty(cancelText))
        {
            CancelButton.IsVisible = false;
        }
        else
        {
            CancelButton.Content = cancelText;
            CancelButton.IsVisible = true;
        }
    }

    public void SetHighContrast(bool isWarning)
    {
        if (isWarning)
        {
            ConfirmButton.Background = new SolidColorBrush(Color.Parse("#E74C3C"));
        }
    }

    private void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
