using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Core.Services;
using FinTrack.Data;
using FinTrack.WPF.Controls;

namespace FinTrack.WPF.Views
{
    public partial class InvestmentDetailWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly int _assetId;
        private string _assetSymbol = "";

        public InvestmentDetailWindow(AppDbContext context, int assetId)
        {
            InitializeComponent();
            _context = context;
            _assetId = assetId;
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var asset = await _context.InvestmentAssets.FindAsync(_assetId);
                if (asset == null)
                {
                    MessageBox.Show("Varlık bulunamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                _assetSymbol = asset.Symbol?.ToUpperInvariant() ?? "";

                // Üst panel bilgileri
                AssetNameText.Text = asset.Name;
                AssetSymbolText.Text = $"({asset.Symbol})";
                AssetCategoryText.Text = asset.Category ?? "Diğer";
                TotalAmountText.Text = $"{asset.TotalAmount:N2}";
                AvgCostText.Text = $"₺{asset.AverageCost:N2}";

                decimal currentPrice = PricingService.GetCurrentPrice(asset.Symbol ?? "", asset.AverageCost);
                CurrentPriceText.Text = $"₺{currentPrice:N2}";

                // İşlem geçmişi
                var transactions = await _context.InvestmentTransactions
                    .Include(t => t.LinkedBankAccount)
                    .Include(t => t.LinkedCreditCardAccount)
                    .Where(t => t.InvestmentAssetId == _assetId)
                    .OrderByDescending(t => t.Date)
                    .ToListAsync();

                var viewModels = transactions.Select(t => new TransactionDetailViewModel
                {
                    Date = t.Date,
                    TypeText = t.Type switch
                    {
                        InvestmentTransactionType.Buy => "🟢 Alım",
                        InvestmentTransactionType.Sell => "🔴 Satım",
                        InvestmentTransactionType.Dividend => "💰 Temettü",
                        InvestmentTransactionType.BonusShare => "🔄 Bedelsiz",
                        InvestmentTransactionType.RightsIssue => "📋 Bedelli",
                        InvestmentTransactionType.Split => "✂️ Bölünme",
                        _ => "❓"
                    },
                    TypeColor = t.Type switch
                    {
                        InvestmentTransactionType.Buy => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")),
                        InvestmentTransactionType.Sell => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),
                        InvestmentTransactionType.Dividend => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")),
                        InvestmentTransactionType.BonusShare => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                        InvestmentTransactionType.RightsIssue => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E44AD")),
                        InvestmentTransactionType.Split => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1ABC9C")),
                        _ => new SolidColorBrush(Colors.Gray)
                    },
                    Amount = t.Amount,
                    UnitPrice = t.UnitPrice,
                    Fee = t.Fee,
                    TotalCost = t.TotalCost,
                    AccountName = GetAccountName(t)
                }).ToList();

                TransactionsGrid.ItemsSource = viewModels;

                // Alt özet hesaplamaları
                decimal totalBuy = transactions.Where(t => t.Type == InvestmentTransactionType.Buy).Sum(t => t.TotalCost);
                decimal totalSell = transactions.Where(t => t.Type == InvestmentTransactionType.Sell).Sum(t => t.TotalCost);
                decimal totalDividend = transactions.Where(t => t.Type == InvestmentTransactionType.Dividend).Sum(t => t.TotalCost);
                decimal totalFee = transactions.Sum(t => t.Fee);

                // Net kar/zarar: Satış gelirleri + Mevcut portföy değeri + Temettü gelirleri - Toplam alım maliyeti
                decimal currentPortfolioValue = asset.TotalAmount * currentPrice;
                decimal netProfit = totalSell + currentPortfolioValue + totalDividend - totalBuy;

                TotalBuyText.Text = $"₺{totalBuy:N2}";
                TotalSellText.Text = $"₺{totalSell:N2}";
                TotalFeeText.Text = $"₺{totalFee:N2}";
                NetProfitText.Text = netProfit >= 0 ? $"+₺{netProfit:N2}" : $"-₺{Math.Abs(netProfit):N2}";
                NetProfitText.Foreground = netProfit >= 0
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));

                // Temettü toplamı varsa göster
                if (totalDividend > 0)
                    TotalFeeText.Text = $"₺{totalFee:N2} | 💰 Temettü: ₺{totalDividend:N2}";

                Title = $"Yatırım Detayı — {asset.Name} ({asset.Symbol})";

                // Sparkline grafiğini yükle
                await LoadChartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veriler yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadChartAsync()
        {
            if (string.IsNullOrWhiteSpace(_assetSymbol)) return;

            try
            {
                int days = GetSelectedPeriodDays();
                DateTime startDate = days > 0 ? DateTime.Today.AddDays(-days) : DateTime.MinValue;

                var priceHistory = await _context.PriceHistories
                    .Where(h => h.Symbol == _assetSymbol && h.Date >= startDate)
                    .OrderBy(h => h.Date)
                    .ToListAsync();

                if (priceHistory.Count < 2)
                {
                    PriceChart.Data = null;
                    return;
                }

                var sparkData = priceHistory.Select(h => new SparklinePoint
                {
                    Date = h.Date,
                    Close = h.ClosePrice,
                    Low = h.LowPrice,
                    High = h.HighPrice
                }).ToList();

                PriceChart.Data = sparkData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chart yüklenirken hata: {ex.Message}");
            }
        }

        private int GetSelectedPeriodDays()
        {
            if (Period30.IsChecked == true) return 30;
            if (Period90.IsChecked == true) return 90;
            if (Period365.IsChecked == true) return 365;
            return 0; // Tümü
        }

        private async void Period_Changed(object sender, RoutedEventArgs e)
        {
            if (_context != null && !string.IsNullOrWhiteSpace(_assetSymbol))
                await LoadChartAsync();
        }

        private string GetAccountName(InvestmentTransaction t)
        {
            if (t.LinkedBankAccount != null)
                return $"🏦 {t.LinkedBankAccount.BankName}";
            if (t.LinkedCreditCardAccount != null)
                return $"💳 {t.LinkedCreditCardAccount.BankName}";
            return "Nakit";
        }

        private async void AddDividend_Click(object sender, RoutedEventArgs e)
        {
            var dividendWin = new AddDividendWindow(_context, _assetId);
            dividendWin.Owner = this;
            if (dividendWin.ShowDialog() == true)
            {
                await LoadDataAsync();
            }
        }

        private async void CapitalAction_Click(object sender, RoutedEventArgs e)
        {
            var capitalWin = new CapitalActionWindow(_context, _assetId);
            capitalWin.Owner = this;
            if (capitalWin.ShowDialog() == true)
            {
                await LoadDataAsync();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class TransactionDetailViewModel
    {
        public DateTime Date { get; set; }
        public required string TypeText { get; set; }
        public required SolidColorBrush TypeColor { get; set; }
        public decimal Amount { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Fee { get; set; }
        public decimal TotalCost { get; set; }
        public required string AccountName { get; set; }
    }
}
