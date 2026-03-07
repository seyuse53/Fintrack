using System.Windows;

namespace FinTrack.WPF.Views
{
    public partial class InfoDialogWindow : Window
    {
        public InfoDialogWindow(string title, string message, string okText = "Tamam")
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;
            OkButton.Content = okText;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
