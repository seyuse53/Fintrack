using System;
using System.Windows;
using FinTrack.Core.Services;

namespace FinTrack.WPF.Views
{
    public partial class UpdatePricesWindow : Window
    {
        public UpdatePricesWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var symbol = SymbolTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    MessageBox.Show("Lütfen geçerli bir sembol giriniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (decimal.TryParse(PriceTextBox.Text.Trim(), out decimal newPrice))
                {
                    PricingService.SetPrice(symbol, newPrice);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Lütfen geçerli bir fiyat giriniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fiyat güncellenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
