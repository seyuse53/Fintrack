using System;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.WPF.Views
{
    public partial class EditInvestmentAssetWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly InvestmentAsset _asset;

        public EditInvestmentAssetWindow(AppDbContext context, InvestmentAsset asset)
        {
            InitializeComponent();
            _context = context;
            _asset = asset;

            // Mevcut değerleri doldur
            SymbolTextBox.Text = asset.Symbol;
            NameTextBox.Text = asset.Name;

            // Kategori eşleştir
            foreach (ComboBoxItem item in CategoryComboBox.Items)
            {
                if (item.Content?.ToString() == asset.Category)
                {
                    CategoryComboBox.SelectedItem = item;
                    break;
                }
            }

            if (CategoryComboBox.SelectedItem == null)
                CategoryComboBox.SelectedIndex = 5; // "Diğer"
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string symbol = SymbolTextBox.Text.Trim().ToUpper();
                string name = NameTextBox.Text.Trim();
                string category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Diğer";

                if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Lütfen sembol ve isim alanlarını doldurunuz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _asset.Symbol = symbol;
                _asset.Name = name;
                _asset.Category = category;

                _context.InvestmentAssets.Update(_asset);
                await _context.SaveChangesAsync();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
