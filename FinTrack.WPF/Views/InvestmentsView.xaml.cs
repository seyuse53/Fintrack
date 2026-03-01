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
    public partial class InvestmentsView : UserControl
    {
        private AppDbContext _context = null!;

        public InvestmentsView()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync(AppDbContext context)
        {
            _context = context;
            await LoadInvestmentsAsync();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_context != null)
            {
                var apiProvider = SettingsManager.GetApiProvider();
                if (apiProvider == ApiProviderType.YahooFinance)
                {
                    FetchApiPricesButton.Visibility = Visibility.Visible;
                }
                else
                {
                    FetchApiPricesButton.Visibility = Visibility.Collapsed;
                }

                await LoadInvestmentsAsync();
            }
        }

        public async Task LoadInvestmentsAsync()
        {
            if (_context == null) return;

            try
            {
                var assets = await _context.InvestmentAssets.ToListAsync();
                
                decimal totalPortfolioValue = 0;
                decimal totalCostBasis = 0;
                
                var viewModels = new List<InvestmentViewModel>();

                foreach (var asset in assets)
                {
                    decimal currentPrice = PricingService.GetCurrentPrice(asset.Symbol, asset.AverageCost);
                    
                    decimal currentValue = asset.TotalAmount * currentPrice;

                    decimal totalCost = asset.TotalAmount * asset.AverageCost;
                    decimal profitLoss = currentValue - totalCost;
                    
                    totalPortfolioValue += currentValue;
                    totalCostBasis += totalCost;

                    viewModels.Add(new InvestmentViewModel
                    {
                        Id = asset.Id,
                        Name = asset.Name,
                        Symbol = asset.Symbol,
                        Category = asset.Category ?? "Diğer",
                        AmountText = $"{asset.TotalAmount:N2} {asset.Symbol}",
                        AverageCost = asset.AverageCost,
                        CurrentPrice = currentPrice,
                        CurrentValue = currentValue,
                        ProfitText = profitLoss >= 0 ? $"+₺{profitLoss:N2}" : $"-₺{Math.Abs(profitLoss):N2}",
                        ProfitColor = profitLoss >= 0 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"))
                    });
                }

                InvestmentsItemsControl.ItemsSource = viewModels;
                
                TotalPortfolioValueText.Text = $"₺{totalPortfolioValue:N2}";
                TotalCostBasisText.Text = $"₺{totalCostBasis:N2}";
                
                decimal totalProfit = totalPortfolioValue - totalCostBasis;
                decimal profitPercentage = totalCostBasis > 0 ? (totalProfit / totalCostBasis) * 100 : 0;
                
                TotalProfitText.Text = totalProfit >= 0 ? $"+₺{totalProfit:N2}" : $"-₺{Math.Abs(totalProfit):N2}";
                TotalProfitText.Foreground = totalProfit >= 0 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));
                
                TotalProfitPercentageText.Text = totalProfit >= 0 ? $"(+%{profitPercentage:N1})" : $"(%{profitPercentage:N1})";
                TotalProfitPercentageText.Foreground = TotalProfitText.Foreground;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yatırımlar yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpdatePricesButton_Click(object sender, RoutedEventArgs e)
        {
            var updateWin = new UpdatePricesWindow();
            updateWin.Owner = Window.GetWindow(this);
            if (updateWin.ShowDialog() == true)
            {
                await LoadInvestmentsAsync();
            }
        }

        private async void ManageInvestmentsButton_Click(object sender, RoutedEventArgs e)
        {
            var manageWin = new ManageInvestmentsWindow(_context);
            manageWin.Owner = Window.GetWindow(this);
            manageWin.ShowDialog();
            
            // Kullanıcı pencereyi kapatınca ana sayfadaki verileri tekrar yenile
            await LoadInvestmentsAsync();
        }

        private async void FetchApiPricesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_context == null) return;

            FetchApiPricesButton.IsEnabled = false;
            FetchApiPricesButton.Content = "⏳ Fiyatlar Çekiliyor...";

            try
            {
                var assets = await _context.InvestmentAssets.ToListAsync();
                var distinctSymbols = assets.Select(a => a.Symbol).Distinct().ToList();

                var tasks = distinctSymbols.Select(symbol => PricingService.FetchRealTimePriceAsync(symbol)).ToList();
                
                // Paralel olarak tüm sembollerin fiyatlarını bekliyoruz
                await Task.WhenAll(tasks);

                // Ekranı güncelle
                await LoadInvestmentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fiyatlar güncellenirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                FetchApiPricesButton.IsEnabled = true;
                FetchApiPricesButton.Content = "🌐 Fiyatları Çek (API)";
            }
        }
    }

    public class InvestmentViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Symbol { get; set; }
        public required string Category { get; set; }
        public required string AmountText { get; set; }
        public decimal AverageCost { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal CurrentValue { get; set; }
        public required string ProfitText { get; set; }
        public required SolidColorBrush ProfitColor { get; set; }
    }
}
