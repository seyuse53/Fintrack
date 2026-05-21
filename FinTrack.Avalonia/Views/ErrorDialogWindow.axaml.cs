using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FinTrack.Avalonia.Views
{
    public partial class ErrorDialogWindow : Window
    {
        public ErrorDialogWindow()
        {
            InitializeComponent();
        }

        public ErrorDialogWindow(string message, string title = "Hata", string? details = null) : this()
        {
            var titleBlock = this.FindControl<TextBlock>("TitleText");
            var messageBlock = this.FindControl<TextBlock>("MessageText");
            var detailsBlock = this.FindControl<TextBlock>("DetailsText");

            if (titleBlock != null) titleBlock.Text = title;
            if (messageBlock != null) messageBlock.Text = message;
            
            if (detailsBlock != null)
            {
                if (string.IsNullOrEmpty(details))
                {
                    detailsBlock.IsVisible = false;
                }
                else
                {
                    detailsBlock.Text = details;
                    detailsBlock.IsVisible = true;
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
