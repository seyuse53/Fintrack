using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FinTrack.Core.Models;
using FinTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WPF.Views
{
    public partial class ReportsView : UserControl
    {
        private AppDbContext _context = null!;

        public ReportsView()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync(AppDbContext context)
        {
            _context = context;

            // Varsayılan: Mevcut ayı göster (Dashboard gibi, ama burada kullanıcı değiştirebilir)
            var now = DateTime.Now;
            StartDatePicker.SelectedDate = new DateTime(now.Year, now.Month, 1);
            EndDatePicker.SelectedDate = now.Date;

            await LoadDataAsync();
        }

        private async void FetchButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            if (_context == null) return;
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Lütfen Başlangıç ve Bitiş tarihlerini seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var startDate = StartDatePicker.SelectedDate.Value.Date;
            var endDate = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);

            try
            {
                // 1. TEMEL VERİLERİ ÇEK
                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                        .ThenInclude(c => c!.ParentCategory)
                    .Where(t => t.Date >= startDate && t.Date <= endDate)
                    .ToListAsync();

                var incomeList = transactions.Where(t => t.Category?.Type == TransactionType.Income).ToList();
                var expenseList = transactions.Where(t => t.Category?.Type == TransactionType.Expense).ToList();

                decimal totalIncome = incomeList.Sum(t => t.Amount);
                decimal totalExpense = expenseList.Sum(t => t.Amount);
                decimal balance = totalIncome - totalExpense;

                IncomeCardText.Text = $"₺{totalIncome:N2}";
                ExpenseCardText.Text = $"₺{totalExpense:N2}";
                BalanceCardText.Text = $"₺{balance:N2}";

                // 2. HARCAMA DAĞILIMI ANALİZİ
                AnalyzeSpending(expenseList);

                // 3. TREND ANALİZİ (SON 6 AY)
                await AnalyzeTrendAsync();

                // 4. YATIRIM PORTFÖY ANALİZİ
                await AnalyzeInvestmentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rapor verileri yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AnalyzeSpending(List<Transaction> expenses)
        {
            if (!expenses.Any())
            {
                TopCategoriesList.ItemsSource = null;
                FullBreakdownGrid.ItemsSource = null;
                return;
            }

            decimal totalExpense = expenses.Sum(e => e.Amount);

            var breakdown = expenses
                .GroupBy(e => e.Category?.Name ?? "Diğer")
                .Select(g => new BreakdownItem
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(e => e.Amount),
                    Count = g.Count(),
                    Percentage = (double)(g.Sum(e => e.Amount) / totalExpense * 100),
                    Color = GetVibrantColor(g.Key)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            FullBreakdownGrid.ItemsSource = breakdown;
            TopCategoriesList.ItemsSource = breakdown.Take(5).ToList();
        }

        private async Task AnalyzeTrendAsync()
        {
            var trends = new List<TrendItem>();
            var now = DateTime.Now;

            // Son 6 ayı hesapla
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var startOfMonth = new DateTime(monthDate.Year, monthDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

                var monthTransactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.Date >= startOfMonth && t.Date <= endOfMonth)
                    .ToListAsync();

                decimal inc = monthTransactions.Where(t => t.Category?.Type == TransactionType.Income).Sum(t => t.Amount);
                decimal exp = monthTransactions.Where(t => t.Category?.Type == TransactionType.Expense).Sum(t => t.Amount);

                trends.Add(new TrendItem
                {
                    MonthLabel = monthDate.ToString("MMM yyyy"),
                    IncomeAmount = inc,
                    ExpenseAmount = exp,
                    // Ölçeklendirme: Max 250px yükseklik (XAML'deki konfigürasyona göre)
                    // Önce tüm veriler içindeki en büyüğü bulup ona göre oranlamak daha sağlıklı olur ama şimdilik basit bir sabit ölçek kullanalım
                });
            }

            // Basit ölçeklendirme mantığı (En yüksek değere göre oranla)
            decimal maxVal = trends.Max(t => Math.Max(t.IncomeAmount, t.ExpenseAmount));
            if (maxVal > 0)
            {
                foreach (var item in trends)
                {
                    item.IncomeHeight = (double)(item.IncomeAmount / maxVal * 250);
                    item.ExpenseHeight = (double)(item.ExpenseAmount / maxVal * 250);
                }
            }

            MonthlyTrendsList.ItemsSource = trends;
        }

        private async Task AnalyzeInvestmentsAsync()
        {
            var assets = await _context.InvestmentAssets.ToListAsync();
            var invTransactions = await _context.InvestmentTransactions.ToListAsync();

            if (!assets.Any()) return;

            // Bu kısımda basitleştirilmiş bir "Varlık Dağılımı" gösteriyoruz
            // Normalde güncel fiyatlarla çarpılması gerekir ama şimdilik "Maliyet" üzerinden gidelim
            // (Yahoo Finance entegrasyonu dashboard'da olduğu için burada da PricingService kullanılabilir)
            decimal totalValue = 0;
            var allocation = assets
                .GroupBy(a => a.Category ?? "Diğer")
                .Select(g => new
                {
                    AssetCategory = g.Key,
                    // Varlıkların maliyet toplamı (basit analiz)
                    TotalValue = g.Sum(a => a.TotalAmount * a.AverageCost)
                })
                .ToList();

            totalValue = allocation.Sum(x => x.TotalValue);
            
            if (totalValue > 0)
            {
                InvestmentAllocationList.ItemsSource = allocation.Select(x => new
                {
                    x.AssetCategory,
                    FormattedValue = $"₺{x.TotalValue:N2}",
                    Percentage = (double)(x.TotalValue / totalValue * 100)
                }).OrderByDescending(a => a.Percentage).ToList();
            }

            TotalPortfolioValueText.Text = $"₺{totalValue:N2}";
            
            // Kâr Zarar (Basitçe satılanlardan edilen kâr + mevcut varlık değer kazancı denebilir)
            // Şimdilik sadece "Sell" işlemlerindeki kârı veya basit bir farkı gösterebiliriz.
            // Örnek olarak sabit bir yeşil renk ve toplam maliyeti gösterelim.
            TotalInvestmentProfitText.Text = "Analiz Ediliyor..."; 
            TotalInvestmentProfitText.Foreground = Brushes.Gray;
        }

        private Brush GetVibrantColor(string categoryName)
        {
            // Kategorilere göre sabit ama canlı renkler döndür
            return categoryName switch
            {
                "Gıda" or "Market" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22")),
                "Kira" or "Ev" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2980B9")),
                "Ulaşım" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1ABC9C")),
                "Eğlence" or "Sosyal" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                "Faturalar" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),
                "Sağlık" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34495E"))
            };
        }
    }

    public class BreakdownItem
    {
        public string CategoryName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string FormattedAmount => $"₺{TotalAmount:N2}";
        public Brush Color { get; set; } = Brushes.SkyBlue;
    }

    public class TrendItem
    {
        public string MonthLabel { get; set; } = "";
        public decimal IncomeAmount { get; set; }
        public decimal ExpenseAmount { get; set; }
        public double IncomeHeight { get; set; }
        public double ExpenseHeight { get; set; }
    }
}
