using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Models;
using FinTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Avalonia.ViewModels
{
    public partial class ReportsViewModel : ViewModelBase
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private DateTime? _startDate;

        [ObservableProperty]
        private DateTime? _endDate;

        [ObservableProperty]
        private string _totalIncomeText = "₺0,00";

        [ObservableProperty]
        private string _totalExpenseText = "₺0,00";

        [ObservableProperty]
        private string _netBalanceText = "₺0,00";

        [ObservableProperty]
        private ObservableCollection<BreakdownItem> _topCategories = new();

        [ObservableProperty]
        private ObservableCollection<BreakdownItem> _fullBreakdown = new();

        [ObservableProperty]
        private ObservableCollection<TrendItem> _monthlyTrends = new();

        [ObservableProperty]
        private string _totalPortfolioValueText = "₺0,00";

        [ObservableProperty]
        private string _totalInvestmentProfitText = "Analiz Ediliyor...";

        [ObservableProperty]
        private string _totalInvestmentProfitColor = "#7F8C8D";

        [ObservableProperty]
        private ObservableCollection<BreakdownItem> _topIncomeCategories = new();

        [ObservableProperty]
        private ObservableCollection<BreakdownItem> _fullIncomeBreakdown = new();

        [ObservableProperty]
        private ObservableCollection<InvestmentAllocationItem> _investmentAllocations = new();

        public ReportsViewModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            var now = DateTime.Now;
            StartDate = new DateTime(now.Year, now.Month, 1);
            EndDate = now.Date;

            await FetchDataAsync();
        }

        [RelayCommand]
        private async Task FetchDataAsync()
        {
            if (_context == null || !StartDate.HasValue || !EndDate.HasValue)
                return;

            var startDate = StartDate.Value.Date;
            var endDate = EndDate.Value.Date.AddDays(1).AddTicks(-1);

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

                TotalIncomeText = $"₺{totalIncome:N2}";
                TotalExpenseText = $"₺{totalExpense:N2}";
                NetBalanceText = $"₺{balance:N2}";

                // 2. HARCAMA VE GELİR DAĞILIMI ANALİZİ
                AnalyzeSpending(expenseList);
                AnalyzeIncome(incomeList);

                // 3. TREND ANALİZİ (SON 6 AY)
                await AnalyzeTrendAsync();

                // 4. YATIRIM PORTFÖY ANALİZİ
                await AnalyzeInvestmentsAsync();
            }
            catch
            {
                // Hata UI tarafında loglanabilir veya diyalog gösterilebilir.
            }
        }

        private void AnalyzeIncome(List<Transaction> incomes)
        {
            if (!incomes.Any())
            {
                TopIncomeCategories.Clear();
                FullIncomeBreakdown.Clear();
                return;
            }

            decimal totalIncome = incomes.Sum(i => i.Amount);

            var breakdown = incomes
                .GroupBy(i => i.Category?.Name ?? "Diğer")
                .Select(g => new BreakdownItem
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(i => i.Amount),
                    Count = g.Count(),
                    Percentage = (double)(g.Sum(i => i.Amount) / totalIncome * 100),
                    ColorHex = GetVibrantColorHex(g.Key)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            FullIncomeBreakdown = new ObservableCollection<BreakdownItem>(breakdown);
            TopIncomeCategories = new ObservableCollection<BreakdownItem>(breakdown.Take(5));
        }

        private void AnalyzeSpending(List<Transaction> expenses)
        {
            if (!expenses.Any())
            {
                TopCategories.Clear();
                FullBreakdown.Clear();
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
                    ColorHex = GetVibrantColorHex(g.Key)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            FullBreakdown = new ObservableCollection<BreakdownItem>(breakdown);
            TopCategories = new ObservableCollection<BreakdownItem>(breakdown.Take(5));
        }

        private async Task AnalyzeTrendAsync()
        {
            var trends = new List<TrendItem>();
            var endDateAnchor = EndDate ?? DateTime.Now;

            // Son 6 ayı hesapla
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = endDateAnchor.AddMonths(-i);
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
                });
            }

            // Basit ölçeklendirme mantığı (En yüksek değere göre oranla)
            decimal maxVal = trends.Max(t => Math.Max(t.IncomeAmount, t.ExpenseAmount));
            if (maxVal > 0)
            {
                foreach (var item in trends)
                {
                    // Max height is 250px in the UI
                    item.IncomeHeight = (double)(item.IncomeAmount / maxVal * 250);
                    item.ExpenseHeight = (double)(item.ExpenseAmount / maxVal * 250);
                }
            }
            else
            {
                 foreach (var item in trends)
                 {
                     item.IncomeHeight = 0;
                     item.ExpenseHeight = 0;
                 }
            }

            MonthlyTrends = new ObservableCollection<TrendItem>(trends);
        }

        private async Task AnalyzeInvestmentsAsync()
        {
            var assets = await _context.InvestmentAssets.ToListAsync();

            if (!assets.Any()) return;

            var allocation = assets
                .GroupBy(a => a.Category ?? "Diğer")
                .Select(g => new InvestmentAllocationItem
                {
                    AssetCategory = g.Key,
                    TotalValue = g.Sum(a => a.TotalAmount * (a.LastKnownPrice > 0 ? a.LastKnownPrice : a.AverageCost))
                })
                .ToList();

            decimal totalCost = assets.Sum(a => a.TotalAmount * a.AverageCost);
            decimal totalCurrentValue = allocation.Sum(x => x.TotalValue);
            decimal totalProfit = totalCurrentValue - totalCost;
            
            if (totalCurrentValue > 0)
            {
                foreach (var item in allocation)
                {
                    item.Percentage = (double)(item.TotalValue / totalCurrentValue * 100);
                }
                InvestmentAllocations = new ObservableCollection<InvestmentAllocationItem>(allocation.OrderByDescending(a => a.Percentage));
            }

            TotalPortfolioValueText = $"₺{totalCurrentValue:N2}";
            
            if (totalProfit > 0)
            {
                TotalInvestmentProfitText = $"+₺{totalProfit:N2}";
                TotalInvestmentProfitColor = "#27AE60"; // Green
            }
            else if (totalProfit < 0)
            {
                TotalInvestmentProfitText = $"-₺{Math.Abs(totalProfit):N2}";
                TotalInvestmentProfitColor = "#E74C3C"; // Red
            }
            else
            {
                TotalInvestmentProfitText = "₺0,00";
                TotalInvestmentProfitColor = "#7F8C8D"; // Gray
            }
        }

        private string GetVibrantColorHex(string categoryName)
        {
            var preset = categoryName switch
            {
                "Gıda" or "Market" => "#E67E22",
                "Kira" or "Ev" => "#2980B9",
                "Ulaşım" => "#1ABC9C",
                "Eğlence" or "Sosyal" => "#9B59B6",
                "Faturalar" => "#E74C3C",
                "Sağlık" => "#27AE60",
                "Maaş" or "Gelir" => "#27AE60",
                _ => null
            };

            if (preset != null) return preset;

            // Generate HSL based on hash of category name
            int hash = 0;
            foreach (char c in categoryName)
            {
                hash = c + (hash << 5) - hash;
            }
            
            double h = Math.Abs(hash % 360);
            double s = 0.65;
            double l = 0.55;
            
            return HslToHex(h, s, l);
        }

        private string HslToHex(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c / 2;

            double r = 0, g = 0, b = 0;

            if (0 <= h && h < 60) { r = c; g = x; b = 0; }
            else if (60 <= h && h < 120) { r = x; g = c; b = 0; }
            else if (120 <= h && h < 180) { r = 0; g = c; b = x; }
            else if (180 <= h && h < 240) { r = 0; g = x; b = c; }
            else if (240 <= h && h < 300) { r = x; g = 0; b = c; }
            else if (300 <= h && h < 360) { r = c; g = 0; b = x; }

            int R = (int)Math.Round((r + m) * 255);
            int G = (int)Math.Round((g + m) * 255);
            int B = (int)Math.Round((b + m) * 255);

            return $"#{R:X2}{G:X2}{B:X2}";
        }
    }

    public class BreakdownItem
    {
        public string CategoryName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string FormattedAmount => $"₺{TotalAmount:N2}";
        public string ColorHex { get; set; } = "#3498DB";
    }

    public class TrendItem
    {
        public string MonthLabel { get; set; } = "";
        public decimal IncomeAmount { get; set; }
        public decimal ExpenseAmount { get; set; }
        public double IncomeHeight { get; set; }
        public double ExpenseHeight { get; set; }
    }

    public class InvestmentAllocationItem
    {
        public string AssetCategory { get; set; } = "";
        public decimal TotalValue { get; set; }
        public double Percentage { get; set; }
        public string FormattedValue => $"₺{TotalValue:N2}";
    }
}
