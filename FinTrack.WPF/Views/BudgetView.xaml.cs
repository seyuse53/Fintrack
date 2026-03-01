using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FinTrack.Core.Models;
using FinTrack.Data;
using FinTrack.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.WPF.Views
{
    public partial class BudgetView : UserControl
    {
        private AppDbContext _db = null!;
        private BudgetService _budgetService = null!;
        private InflationService _inflationService = null!;

        private int _year;
        private int _month;
        private double? _currentCpi;
        private int _cpiActualYear;
        private int _cpiActualMonth;

        public BudgetView()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync(AppDbContext db)
        {
            _db = db;
            _budgetService = new BudgetService(db);
            _inflationService = new InflationService(db);

            _year = DateTime.Now.Year;
            _month = DateTime.Now.Month;

            LoadCategories();
            await RefreshAllAsync();
        }

        private static readonly string[] MonthNames =
        {
            "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
            "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
        };

        private void SetMonthLabel()
        {
            MonthLabel.Text = $"{MonthNames[_month - 1]} {_year}";
        }

        private void LoadCategories()
        {
            var expenseCategories = _db.Categories
                .Where(c => c.Type == TransactionType.Expense)
                .OrderBy(c => c.Name)
                .ToList();
            CategoryCombo.ItemsSource = expenseCategories;
        }

        private async Task RefreshAllAsync()
        {
            SetMonthLabel();

            CpiInfoText.Text = "📈 TÜİK TÜFE yükleniyor...";
            var cpiResult = await _inflationService.GetCpiRateWithPeriodAsync(_year, _month);
            _currentCpi = cpiResult?.Rate;
            _cpiActualYear  = cpiResult?.ActualYear  ?? _year;
            _cpiActualMonth = cpiResult?.ActualMonth ?? _month;
            UpdateCpiBar();

            RefreshBudgetCards();
        }

        private void UpdateCpiBar()
        {
            var lastFetch = _inflationService.LastFetchedAt();
            string fetchInfo = lastFetch.HasValue
                ? $" (güncelleme: {lastFetch.Value.ToLocalTime():dd.MM.yyyy})"
                : "";

            if (_currentCpi.HasValue)
            {
                string periodLabel = $"{MonthNames[_cpiActualMonth - 1]} {_cpiActualYear - 1}→{_cpiActualYear}";
                string lagNote = (_cpiActualYear != _year || _cpiActualMonth != _month)
                    ? " ⚠️ (henüz açıklanmadı, son veri kullanılıyor)"
                    : "";
                CpiInfoText.Text = $"📈 TÜİK TÜFE [{periodLabel}]: %{_currentCpi.Value:F2}{lagNote}{fetchInfo}";
            }
            else
                CpiInfoText.Text = "📈 TÜİK TÜFE: Veri bulunamadı";
        }

        private void RefreshBudgetCards()
        {
            var limits = _budgetService.GetAllLimits();
            var cards = limits.Select(l =>
            {
                var summary = _budgetService.GetSummary(l, _year, _month, _currentCpi);
                return new BudgetCardVm(summary, _year, _month);
            }).ToList();

            BudgetList.ItemsSource = cards;
        }

        private async void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _month--;
            if (_month < 1) { _month = 12; _year--; }
            await RefreshAllAsync();
        }

        private async void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _month++;
            if (_month > 12) { _month = 1; _year++; }
            await RefreshAllAsync();
        }

        private async void RefreshCpi_Click(object sender, RoutedEventArgs e)
        {
            RefreshCpiBtn.IsEnabled = false;
            CpiInfoText.Text = "🔄 TÜİK'ten veri çekiliyor...";

            double? newRate = await _inflationService.RefreshFromTuikAsync(_year, _month);
            _currentCpi = newRate ?? await _inflationService.GetCpiRateAsync(_year, _month);

            UpdateCpiBar();
            RefreshBudgetCards();
            RefreshCpiBtn.IsEnabled = true;
        }

        private void SaveLimit_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryCombo.SelectedValue is not int categoryId)
            {
                MessageBox.Show("Lütfen bir kategori seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string raw = LimitInput.Text.Trim().Replace(".", ",");
            if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                    new System.Globalization.CultureInfo("tr-TR"), out decimal limit) || limit <= 0)
            {
                MessageBox.Show("Geçerli bir limit tutarı girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _budgetService.SaveLimit(categoryId, limit);
            RefreshBudgetCards();
            ClearForm();
        }

        private void DeleteLimit_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryCombo.SelectedValue is not int categoryId)
            {
                MessageBox.Show("Lütfen silinecek kategoriyi seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Bu kategorinin bütçe limitini silmek istiyor musunuz?",
                "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _budgetService.DeleteLimit(categoryId);
                RefreshBudgetCards();
                ClearForm();
            }
        }

        private void ClearEdit_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            CategoryCombo.SelectedIndex = -1;
            LimitInput.Text = string.Empty;
        }
    }

    public class BudgetCardVm
    {
        private readonly BudgetSummary _s;
        private readonly int _year;
        private readonly int _month;

        private static readonly string[] MonthNamesShort =
        {
            "Oca", "Şub", "Mar", "Nis", "May", "Haz",
            "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"
        };

        public BudgetCardVm(BudgetSummary s, int year, int month)
        {
            _s = s;
            _year = year;
            _month = month;
        }

        public string CategoryName => _s.Budget.Category?.Name ?? "—";

        public string StatusIcon => _s.Status switch
        {
            BudgetStatus.Good     => "🟢",
            BudgetStatus.Warning  => "🟡",
            BudgetStatus.Exceeded => "🔴",
            _                     => "⚪"
        };

        public string SpendingVsLimit => _s.Budget.MonthlyLimit > 0
            ? $"₺{_s.Spending:N0} / ₺{_s.Budget.MonthlyLimit:N0}"
            : $"₺{_s.Spending:N0} (limit yok)";

        public double ProgressPercent => Math.Min(_s.ProgressRatio * 100, 100);

        public Brush StatusColor => _s.Status switch
        {
            BudgetStatus.Good     => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
            BudgetStatus.Warning  => new SolidColorBrush(Color.FromRgb(243, 156, 18)),
            BudgetStatus.Exceeded => new SolidColorBrush(Color.FromRgb(192, 57, 43)),
            _                     => new SolidColorBrush(Color.FromRgb(127, 140, 141))
        };

        public string ComparisonText
        {
            get
            {
                if (_s.LastYearSpending == 0)
                    return $"📅 {MonthNamesShort[_month - 1]} {_year - 1}: Veri yok";
                return $"📅 {MonthNamesShort[_month - 1]} {_year - 1}: ₺{_s.LastYearSpending:N0}";
            }
        }

        public string InflationText
        {
            get
            {
                if (_s.LastYearSpending == 0) return string.Empty;
                if (!_s.CpiRate.HasValue)
                    return "📈 Enflasyon verisi mevcut değil";

                string adj = $"₺{_s.InflationAdjustedLastYear:N0} (TÜFE %{_s.CpiRate.Value:F1})";
                if (!_s.RealDifferencePercent.HasValue) return $"📈 {adj}";

                double diff = _s.RealDifferencePercent.Value;
                string sign = diff >= 0 ? "+" : "";
                string verdict = diff <= 0 ? "✅ Tasarruf!" : "⚠️ Artış";
                return $"📈 {adj} → {sign}{diff:F1}% {verdict}";
            }
        }

        public Brush RealDiffColor
        {
            get
            {
                if (_s.RealDifferencePercent is null) return Brushes.Gray;
                return _s.RealDifferencePercent.Value <= 0
                    ? new SolidColorBrush(Color.FromRgb(39, 174, 96))
                    : new SolidColorBrush(Color.FromRgb(192, 57, 43));
            }
        }
    }
}
