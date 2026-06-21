using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Models;
using FinTrack.Data;
using FinTrack.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Avalonia.ViewModels
{
    public partial class BudgetViewModel : ViewModelBase
    {
        private readonly AppDbContext _db;
        private readonly BudgetService _budgetService;
        private readonly InflationService _inflationService;

        private int _year;
        private int _month;
        private double? _currentCpi;
        private int _cpiActualYear;
        private int _cpiActualMonth;

        [ObservableProperty]
        private string _monthYearText = "";

        [ObservableProperty]
        private string _cpiInfoText = "Yükleniyor...";

        [ObservableProperty]
        private bool _isCpiLoading;

        [ObservableProperty]
        private ObservableCollection<Category> _expenseCategories = new();

        [ObservableProperty]
        private Category? _selectedCategory;

        [ObservableProperty]
        private string _limitInputText = "";

        [ObservableProperty]
        private string _suggestedLimitText = "";

        [ObservableProperty]
        private bool _hasSuggestion = false;

        private decimal _suggestedAmount = 0;

        [ObservableProperty]
        private ObservableCollection<BudgetCardVm> _budgetCards = new();

        private static readonly string[] MonthNames =
        {
            "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
            "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
        };

        public BudgetViewModel(AppDbContext db)
        {
            _db = db;
            _budgetService = new BudgetService(db);
            _inflationService = new InflationService(db);

            _year = DateTime.Now.Year;
            _month = DateTime.Now.Month;
        }

        public async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
            await RefreshAllAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _db.Categories
                .Where(c => c.Type == TransactionType.Expense)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ExpenseCategories = new ObservableCollection<Category>(categories);
        }

        private async Task RefreshAllAsync()
        {
            MonthYearText = $"{MonthNames[_month - 1]} {_year}";

            CpiInfoText = "📈 TÜİK TÜFE yükleniyor...";
            IsCpiLoading = true;

            var cpiResult = await _inflationService.GetCpiRateWithPeriodAsync(_year, _month);
            _currentCpi = cpiResult?.Rate;
            _cpiActualYear = cpiResult?.ActualYear ?? _year;
            _cpiActualMonth = cpiResult?.ActualMonth ?? _month;

            UpdateCpiBar();
            RefreshBudgetCards();

            IsCpiLoading = false;
        }

        private void UpdateCpiBar()
        {
            var lastFetch = _inflationService.LastFetchedAt();
            string fetchInfo = lastFetch.HasValue
                ? $" (güncelleme: {lastFetch.Value.ToLocalTime():dd.MM.yyyy})"
                : "";

            if (_currentCpi.HasValue)
            {
                string periodLabel = $"{MonthNames[_cpiActualMonth - 1]} {_cpiActualYear} (Yıllık)";
                string lagNote = (_cpiActualYear != _year || _cpiActualMonth != _month)
                    ? " ⚠️ (henüz açıklanmadı, son veri kullanılıyor)"
                    : "";
                CpiInfoText = $"📈 TÜİK TÜFE [{periodLabel}]: %{_currentCpi.Value:F2}{lagNote}{fetchInfo}";
            }
            else
            {
                CpiInfoText = "📈 TÜİK TÜFE: Veri bulunamadı";
            }
        }

        private void RefreshBudgetCards()
        {
            var limits = _budgetService.GetAllLimits();
            var cards = limits.Select(l =>
            {
                var summary = _budgetService.GetSummary(l, _year, _month, _currentCpi);
                return new BudgetCardVm(summary, _year, _month);
            }).ToList();

            BudgetCards = new ObservableCollection<BudgetCardVm>(cards);
        }

        [RelayCommand]
        private async Task PrevMonthAsync()
        {
            _month--;
            if (_month < 1) { _month = 12; _year--; }
            await RefreshAllAsync();
        }

        [RelayCommand]
        private async Task NextMonthAsync()
        {
            _month++;
            if (_month > 12) { _month = 1; _year++; }
            await RefreshAllAsync();
        }

        [RelayCommand]
        private async Task RefreshCpiAsync()
        {
            IsCpiLoading = true;
            CpiInfoText = "🔄 TÜİK'ten veri çekiliyor...";

            double? newRate = await _inflationService.RefreshFromTuikAsync(_year, _month);
            _currentCpi = newRate ?? await _inflationService.GetCpiRateAsync(_year, _month);

            UpdateCpiBar();
            RefreshBudgetCards();
            IsCpiLoading = false;
        }

        [RelayCommand]
        private void SaveLimit()
        {
            if (SelectedCategory == null)
                return; // Normalde Dialog göstermeliyiz ama şimdilik sessiz dönelim (View tarafı uyarır)

            string raw = LimitInputText.Trim().Replace(".", ",");
            if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                    new System.Globalization.CultureInfo("tr-TR"), out decimal limit) || limit <= 0)
            {
                return;
            }

            _budgetService.SaveLimit(SelectedCategory.Id, limit);
            RefreshBudgetCards();
            ClearForm();
        }

        [RelayCommand]
        private void DeleteLimit()
        {
            if (SelectedCategory == null) return;
            
            _budgetService.DeleteLimit(SelectedCategory.Id);
            RefreshBudgetCards();
            ClearForm();
        }

        [RelayCommand]
        private void ClearForm()
        {
            SelectedCategory = null;
            LimitInputText = string.Empty;
            HasSuggestion = false;
        }

        [RelayCommand]
        private void ApplySuggestion()
        {
            LimitInputText = _suggestedAmount.ToString("N2");
        }

        partial void OnSelectedCategoryChanged(Category? value)
        {
            if (value != null)
            {
                var limit = _budgetService.GetLimit(value.Id);
                LimitInputText = limit != null ? limit.MonthlyLimit.ToString("N2") : string.Empty;

                decimal lastYear = _budgetService.GetSameMonthLastYearSpending(value.Id, _year, _month);
                if (lastYear > 0)
                {
                    decimal suggested = _currentCpi.HasValue 
                        ? lastYear * (1 + (decimal)(_currentCpi.Value / 100.0)) 
                        : lastYear;
                    
                    SuggestedLimitText = $"💡 Öneri (Geçen Yıl + Enflasyon): ₺{suggested:N0}";
                    _suggestedAmount = suggested;
                    HasSuggestion = true;
                }
                else
                {
                    HasSuggestion = false;
                }
            }
            else
            {
                LimitInputText = string.Empty;
                HasSuggestion = false;
            }
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
            BudgetStatus.Good => "🟢",
            BudgetStatus.Warning => "🟡",
            BudgetStatus.Exceeded => "🔴",
            _ => "⚪"
        };

        public string SpendingVsLimit => _s.Budget.MonthlyLimit > 0
            ? $"₺{_s.Spending:N0} / ₺{_s.Budget.MonthlyLimit:N0}"
            : $"₺{_s.Spending:N0} (limit yok)";

        public double ProgressPercent => Math.Min(_s.ProgressRatio * 100, 100);

        // Avalonia Binding için Brush Hex Renk Kodları
        public string StatusColorHex => _s.Status switch
        {
            BudgetStatus.Good => "#27AE60",
            BudgetStatus.Warning => "#F39C12",
            BudgetStatus.Exceeded => "#C0392B",
            _ => "#7F8C8D"
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

        public string RealDiffColorHex
        {
            get
            {
                if (_s.RealDifferencePercent is null) return "#7F8C8D";
                return _s.RealDifferencePercent.Value <= 0 ? "#27AE60" : "#C0392B";
            }
        }
    }
}
