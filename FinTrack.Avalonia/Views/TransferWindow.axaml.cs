using Avalonia.Controls;
using Avalonia.Interactivity;
using FinTrack.Avalonia.ViewModels;

namespace FinTrack.Avalonia.Views;

public partial class TransferWindow : Window
{
    public TransferWindow()
    {
        InitializeComponent();
    }

    public TransferWindow(int? initialSourceAccountId) : this()
    {
        var vm = new TransferViewModel(initialSourceAccountId);
        vm.CloseAction = () => Close(true);
        DataContext = vm;
    }

    private void AmountTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var text = textBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            string cleanText = text.Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(cleanText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amount))
            {
                var trCulture = new System.Globalization.CultureInfo("tr-TR");
                textBox.Text = amount.ToString("N2", trCulture);
            }
        }
    }
}
