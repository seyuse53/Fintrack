using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FinTrack.Avalonia.Views
{
    public partial class InfoDialogWindow : Window
    {
        public InfoDialogWindow()
        {
            InitializeComponent();
        }

        public InfoDialogWindow(string message, string title = "Bilgi") : this()
        {
            var titleBlock = this.FindControl<TextBlock>("TitleText");
            var messageBlock = this.FindControl<TextBlock>("MessageText");

            if (titleBlock != null) titleBlock.Text = title;
            if (messageBlock != null) messageBlock.Text = message;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
