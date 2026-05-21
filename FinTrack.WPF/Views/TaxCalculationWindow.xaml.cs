using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Core.Services;
using FinTrack.Data;

namespace FinTrack.WPF.Views
{
    public partial class TaxCalculationWindow : Window
    {
        private readonly AppDbContext _context;

        public TaxCalculationWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            Loaded += async (s, e) => await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // Yıl listesini doldur
            var years = await _context.InvestmentTransactions
                .Select(t => t.Date.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            if (!years.Contains(DateTime.Now.Year))
                years.Insert(0, DateTime.Now.Year);

            years = years.OrderByDescending(y => y).ToList();

            foreach (var year in years)
            {
                YearComboBox.Items.Add(new ComboBoxItem { Content = year.ToString(), Tag = year });
            }

            if (YearComboBox.Items.Count > 0)
            {
                YearComboBox.SelectedIndex = 0;
            }
        }

        private async void Year_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (YearComboBox.SelectedItem is ComboBoxItem selected && selected.Tag is int year)
            {
                await CalculateTaxAsync(year);
            }
        }

        private async Task CalculateTaxAsync(int year)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                // Satış ve temettü işlemlerini çek
                var transactions = await _context.InvestmentTransactions
                    .Include(t => t.InvestmentAsset)
                    .Where(t => t.Date >= startDate && t.Date <= endDate &&
                                (t.Type == InvestmentTransactionType.Sell ||
                                 t.Type == InvestmentTransactionType.Dividend))
                    .OrderBy(t => t.Date)
                    .ToListAsync();

                var taxItems = new List<TaxDetailViewModel>();
                decimal totalGain = 0;
                decimal totalTax = 0;
                decimal totalDividend = 0;

                foreach (var tx in transactions)
                {
                    var asset = tx.InvestmentAsset;
                    if (asset == null) continue;

                    string category = asset.Category ?? "Diğer";

                    if (tx.Type == InvestmentTransactionType.Sell)
                    {
                        // Alım maliyeti: ortalama maliyet × satılan adet
                        // Not: Ortalama maliyet, satış anındaki asset.AverageCost değil, 
                        // gerçek maliyet olmalı. Şimdilik AverageCost kullanıyoruz.
                        decimal buyCost = tx.Amount * asset.AverageCost;
                        decimal sellRevenue = tx.TotalCost;
                        decimal gain = sellRevenue - buyCost;
                        decimal taxRate = SettingsManager.GetTaxRateForCategory(category);
                        decimal taxAmount = gain > 0 ? gain * (taxRate / 100m) : 0;

                        totalGain += gain;
                        totalTax += taxAmount;

                        taxItems.Add(new TaxDetailViewModel
                        {
                            Date = tx.Date,
                            AssetName = $"{asset.Symbol} ({asset.Name})",
                            TypeText = "🔴 Satış",
                            TypeColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),
                            Amount = tx.Amount,
                            BuyCost = buyCost,
                            SellRevenue = sellRevenue,
                            Gain = gain,
                            GainText = gain >= 0 ? $"+₺{gain:N2}" : $"-₺{Math.Abs(gain):N2}",
                            GainColor = gain >= 0
                                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),
                            TaxRate = taxRate,
                            TaxRateText = $"%{taxRate:N0}",
                            TaxAmount = taxAmount
                        });
                    }
                    else if (tx.Type == InvestmentTransactionType.Dividend)
                    {
                        decimal dividendAmount = tx.TotalCost;
                        decimal dividendTaxRate = SettingsManager.GetDividendTaxRate();
                        decimal dividendTax = dividendAmount * (dividendTaxRate / 100m);

                        totalDividend += dividendAmount;
                        totalGain += dividendAmount;
                        totalTax += dividendTax;

                        taxItems.Add(new TaxDetailViewModel
                        {
                            Date = tx.Date,
                            AssetName = $"{asset.Symbol} ({asset.Name})",
                            TypeText = "💰 Temettü",
                            TypeColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")),
                            Amount = tx.Amount,
                            BuyCost = 0,
                            SellRevenue = dividendAmount,
                            Gain = dividendAmount,
                            GainText = $"+₺{dividendAmount:N2}",
                            GainColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")),
                            TaxRate = dividendTaxRate,
                            TaxRateText = $"%{dividendTaxRate:N0}",
                            TaxAmount = dividendTax
                        });
                    }
                }

                // Özet kartları güncelle
                TotalGainText.Text = totalGain >= 0 ? $"+₺{totalGain:N2}" : $"-₺{Math.Abs(totalGain):N2}";
                TotalGainText.Foreground = totalGain >= 0
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));

                TotalTaxText.Text = $"₺{totalTax:N2}";
                TotalDividendText.Text = $"₺{totalDividend:N2}";

                decimal netReturn = totalGain - totalTax;
                NetReturnText.Text = netReturn >= 0 ? $"+₺{netReturn:N2}" : $"-₺{Math.Abs(netReturn):N2}";
                NetReturnText.Foreground = netReturn >= 0
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2980B9"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));

                TransactionCountText.Text = $"({taxItems.Count} işlem)";
                TaxDetailsGrid.ItemsSource = taxItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Vergi hesaplanırken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class TaxDetailViewModel
    {
        public DateTime Date { get; set; }
        public required string AssetName { get; set; }
        public required string TypeText { get; set; }
        public required SolidColorBrush TypeColor { get; set; }
        public decimal Amount { get; set; }
        public decimal BuyCost { get; set; }
        public decimal SellRevenue { get; set; }
        public decimal Gain { get; set; }
        public required string GainText { get; set; }
        public required SolidColorBrush GainColor { get; set; }
        public decimal TaxRate { get; set; }
        public required string TaxRateText { get; set; }
        public decimal TaxAmount { get; set; }
    }
}
