using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Services;
using FinTrack.Data;

namespace FinTrack.WPF.Views
{
    public partial class UpdatePricesWindow : Window
    {
        private readonly AppDbContext _context;
        private List<PriceUpdateItem> _items = new();

        public UpdatePricesWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            Loaded += async (s, e) => await LoadAssetsAsync();
        }

        private async Task LoadAssetsAsync()
        {
            try
            {
                var assets = await _context.InvestmentAssets
                    .OrderBy(a => a.Symbol)
                    .ToListAsync();

                _items = assets.Select(a =>
                {
                    decimal currentPrice = PricingService.GetCurrentPrice(a.Symbol, a.AverageCost);
                    return new PriceUpdateItem
                    {
                        Symbol = a.Symbol,
                        Name = a.Name,
                        CurrentPriceText = $"₺{currentPrice:N2}",
                        NewPrice = ""
                    };
                }).ToList();

                PricesGrid.ItemsSource = _items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Varlıklar yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int updatedCount = 0;

                foreach (var item in _items)
                {
                    if (!string.IsNullOrWhiteSpace(item.NewPrice))
                    {
                        // Binlik ayraç temizle ve virgülü nokta yap
                        string cleaned = item.NewPrice.Replace(".", "").Replace(",", ".").Trim();
                        if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal newPrice) && newPrice >= 0)
                        {
                            PricingService.SetPrice(item.Symbol, newPrice);
                            updatedCount++;
                        }
                    }
                }

                if (updatedCount > 0)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Güncellenecek fiyat bulunamadı. Lütfen 'Yeni Fiyat' sütununa değer giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Information);
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

    public class PriceUpdateItem : INotifyPropertyChanged
    {
        private string _newPrice = "";

        public string Symbol { get; set; } = "";
        public string Name { get; set; } = "";
        public string CurrentPriceText { get; set; } = "";

        public string NewPrice
        {
            get => _newPrice;
            set
            {
                _newPrice = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewPrice)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
