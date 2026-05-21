using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Core.Services;
using FinTrack.Data;
using FinTrack.WPF.Controls;

namespace FinTrack.WPF.Views
{
    public partial class InvestmentsView : UserControl
    {
        private AppDbContext _context = null!;
        private List<InvestmentViewModel> _allViewModels = new();

        public InvestmentsView()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync(AppDbContext context)
        {
            _context = context;

            // Uygulama açılışında DB'deki son bilinen fiyatları belleğe yükle
            var assets = await _context.InvestmentAssets.ToListAsync();
            PricingService.LoadPricesFromAssets(assets);

            await LoadInvestmentsAsync();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_context != null)
            {
                // Herhangi bir kategori için API seçildiyse butonu göster
                var settings = SettingsManager.LoadSettings();
                bool anyApiEnabled = settings.DovizProvider != ApiProviderType.Manual
                    || settings.AltinProvider != ApiProviderType.Manual
                    || settings.HisseProvider != ApiProviderType.Manual
                    || settings.KriptoProvider != ApiProviderType.Manual
                    || settings.FonProvider != ApiProviderType.Manual
                    || settings.DigerProvider != ApiProviderType.Manual;

                FetchApiPricesButton.Visibility = anyApiEnabled ? Visibility.Visible : Visibility.Collapsed;

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
                
                _allViewModels = new List<InvestmentViewModel>();

                // Mini sparkline için son 7 günlük fiyat verisi
                var sevenDaysAgo = DateTime.Today.AddDays(-7);
                var recentPrices = await _context.PriceHistories
                    .Where(h => h.Date >= sevenDaysAgo)
                    .OrderBy(h => h.Date)
                    .ToListAsync();

                foreach (var asset in assets)
                {
                    decimal currentPrice = PricingService.GetCurrentPrice(asset.Symbol ?? "", asset.AverageCost);
                    
                    decimal currentValue = asset.TotalAmount * currentPrice;

                    decimal totalCost = asset.TotalAmount * asset.AverageCost;
                    decimal profitLoss = currentValue - totalCost;
                    decimal profitPercentage = totalCost > 0 ? (profitLoss / totalCost) * 100 : 0;
                    
                    totalPortfolioValue += currentValue;
                    totalCostBasis += totalCost;

                    // Mini sparkline verisi
                    var symbol = asset.Symbol?.ToUpperInvariant() ?? "";
                    var sparkData = recentPrices
                        .Where(h => h.Symbol == symbol)
                        .Select(h => new SparklinePoint
                        {
                            Date = h.Date,
                            Close = h.ClosePrice,
                            Low = h.LowPrice,
                            High = h.HighPrice
                        })
                        .ToList();

                    _allViewModels.Add(new InvestmentViewModel
                    {
                        Id = asset.Id,
                        Name = asset.Name,
                        Symbol = asset.Symbol ?? "",
                        Category = asset.Category ?? "Diğer",
                        AmountText = $"{asset.TotalAmount:N2} {asset.Symbol}",
                        AverageCost = asset.AverageCost,
                        CurrentPrice = currentPrice,
                        CurrentValue = currentValue,
                        ProfitLoss = profitLoss,
                        ProfitPercentage = profitPercentage,
                        ProfitText = profitLoss >= 0 ? $"+₺{profitLoss:N2}" : $"-₺{Math.Abs(profitLoss):N2}",
                        ProfitColor = profitLoss >= 0 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828")),
                        PortfolioWeight = "—", // Hesaplanacak
                        SparklineData = sparkData.Count >= 2 ? sparkData : null
                    });
                }

                // Portföy ağırlığı hesaplama
                foreach (var vm in _allViewModels)
                {
                    if (totalPortfolioValue > 0)
                        vm.PortfolioWeight = $"%{(vm.CurrentValue / totalPortfolioValue * 100):N1}";
                    else
                        vm.PortfolioWeight = "%0.0";
                }

                // Kategori filtresine göre göster ve özetleri güncelle
                ApplyCategoryFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yatırımlar yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyCategoryFilter()
        {
            if (_allViewModels == null) return;
            string selectedCategory = (CategoryFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tümü";
            
            var filteredList = selectedCategory == "Tümü" 
                ? _allViewModels 
                : _allViewModels.Where(v => v.Category == selectedCategory).ToList();

            InvestmentsItemsControl.ItemsSource = filteredList;

            // Özet kartları sadece filtrelenmiş listeye göre güncelle
            decimal totalPortfolioValue = filteredList.Sum(v => v.CurrentValue);
            decimal totalCostBasis = filteredList.Sum(v => v.AverageCost * decimal.Parse(v.AmountText.Split(' ')[0])); // Approximate cost basis from UI VM

            TotalPortfolioValueText.Text = $"₺{totalPortfolioValue:N2}";
            TotalCostBasisText.Text = $"₺{totalCostBasis:N2}";
            
            decimal totalProfit = totalPortfolioValue - totalCostBasis;
            decimal totalProfitPercentage = totalCostBasis > 0 ? (totalProfit / totalCostBasis) * 100 : 0;
            
            TotalProfitText.Text = totalProfit >= 0 ? $"+₺{totalProfit:N2}" : $"-₺{Math.Abs(totalProfit):N2}";
            TotalProfitText.Foreground = totalProfit >= 0 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));
            
            TotalProfitPercentageText.Text = totalProfit >= 0 ? $"(+%{totalProfitPercentage:N1})" : $"(%{totalProfitPercentage:N1})";
            TotalProfitPercentageText.Foreground = TotalProfitText.Foreground;

            // En kârlı varlık
            var topPerformer = filteredList
                .Where(v => v.CurrentValue > 0)
                .OrderByDescending(v => v.ProfitPercentage)
                .FirstOrDefault();

            if (topPerformer != null)
            {
                TopPerformerNameText.Text = $"{topPerformer.Name} ({topPerformer.Symbol})";
                TopPerformerProfitText.Text = topPerformer.ProfitPercentage >= 0 
                    ? $"+%{topPerformer.ProfitPercentage:N1}" 
                    : $"%{topPerformer.ProfitPercentage:N1}";
                TopPerformerProfitText.Foreground = topPerformer.ProfitPercentage >= 0
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            }
            else
            {
                TopPerformerNameText.Text = "—";
                TopPerformerProfitText.Text = "";
            }
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allViewModels != null && _allViewModels.Count > 0)
            {
                ApplyCategoryFilter();
            }
        }

        private void AssetCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int assetId)
            {
                var detailWindow = new InvestmentDetailWindow(_context, assetId);
                detailWindow.Owner = Window.GetWindow(this);
                detailWindow.ShowDialog();
            }
        }

        private async void UpdatePricesButton_Click(object sender, RoutedEventArgs e)
        {
            var updateWin = new UpdatePricesWindow(_context);
            updateWin.Owner = Window.GetWindow(this);
            if (updateWin.ShowDialog() == true)
            {
                // Fiyatları DB'ye kalıcı olarak kaydet
                await SavePricesToDbAsync();
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

        private void TaxReportButton_Click(object sender, RoutedEventArgs e)
        {
            var taxWin = new TaxCalculationWindow(_context);
            taxWin.Owner = Window.GetWindow(this);
            taxWin.ShowDialog();
        }

        private async void FetchApiPricesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_context == null) return;

            FetchApiPricesButton.IsEnabled = false;
            FetchApiPricesButton.Content = "⏳ Fiyatlar Çekiliyor...";

            try
            {
                var assets = await _context.InvestmentAssets.ToListAsync();

                // Kategori bazlı akıllı rotalama: her varlık kendi API'sinden çekilir
                var tasks = assets.Select(a => PricingService.FetchPriceSmartAsync(a.Symbol, a.Category)).ToList();
                
                // Paralel olarak tüm sembollerin fiyatlarını bekliyoruz
                await Task.WhenAll(tasks);

                int successCount = tasks.Count(t => t.Result);

                // Fiyatları DB'ye kalıcı olarak kaydet (kaynak: API)
                await SavePricesToDbAsync("API");

                // Ekranı güncelle
                await LoadInvestmentsAsync();

                if (successCount == 0 && assets.Count > 0)
                {
                    MessageBox.Show("Hiçbir varlığın fiyatı güncellenemedi. Ayarlar > API sekmesinden sağlayıcı ayarlarını kontrol ediniz.", 
                        "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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

        private async Task SavePricesToDbAsync(string source = "Manuel")
        {
            try
            {
                var assets = await _context.InvestmentAssets.ToListAsync();
                
                // 1. LastKnownPrice güncelle (Seçenek A — hızlı erişim)
                PricingService.SavePricesToAssets(assets);

                // 2. PriceHistory tablosuna kaydet (Seçenek B — günlük geçmiş)
                var today = DateTime.Today;
                var todayHistories = await _context.PriceHistories
                    .Where(h => h.Date == today)
                    .ToListAsync();

                PricingService.RecordPriceHistory(
                    assets,
                    todayHistories,
                    newRecord => _context.PriceHistories.Add(newRecord),
                    source);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fiyatlar DB'ye kaydedilirken hata: {ex.Message}");
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
        public decimal ProfitLoss { get; set; }
        public decimal ProfitPercentage { get; set; }
        public required string ProfitText { get; set; }
        public required SolidColorBrush ProfitColor { get; set; }
        public required string PortfolioWeight { get; set; }
        public List<SparklinePoint>? SparklineData { get; set; }
    }
}
