using System.Windows;
using System.Windows.Media;

namespace FinTrack.WPF
{
    public partial class GeneralConfirmWindow : Window
    {
        public GeneralConfirmWindow(string title, string message, string confirmText = "Evet, Onayla", string cancelText = "Vazgeç")
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;
            ConfirmButton.Content = confirmText;
            
            if (string.IsNullOrEmpty(cancelText))
            {
                CancelButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                CancelButton.Content = cancelText;
            }
        }

        public void SetHighContrast(bool isWarning)
        {
            if (isWarning)
            {
                ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
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
