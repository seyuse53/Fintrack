using System.Windows;

namespace FinTrack.WPF.Views
{
    public partial class ErrorDialogWindow : Window
    {
        public ErrorDialogWindow(string title, string message, string details = "")
        {
            InitializeComponent();
            
            TitleText.Text = title;
            MessageText.Text = message;
            
            if (string.IsNullOrEmpty(details))
            {
                DetailsText.Visibility = Visibility.Collapsed;
            }
            else
            {
                DetailsText.Visibility = Visibility.Visible;
                DetailsText.Text = "Teknik Detaylar: " + details;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
