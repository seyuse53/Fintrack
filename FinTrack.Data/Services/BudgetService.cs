using System;
using System.Linq;
using FinTrack.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Data.Services
{
    public class BudgetService
    {
        private readonly AppDbContext _db;

        public BudgetService(AppDbContext db)
        {
            _db = db;
        }

        // ── CRUD ─────────────────────────────────────────────────────────────

        /// <summary>Returns the budget limit for a category, or null if not set.</summary>
        public BudgetLimit? GetLimit(int categoryId)
        {
            return _db.BudgetLimits
                      .Include(b => b.Category)
                      .FirstOrDefault(b => b.CategoryId == categoryId);
        }

        /// <summary>Returns all defined budget limits with their categories.</summary>
        public System.Collections.Generic.List<BudgetLimit> GetAllLimits()
        {
            return _db.BudgetLimits
                      .Include(b => b.Category)
                      .OrderBy(b => b.Category!.Name)
                      .ToList();
        }

        /// <summary>Creates or updates the monthly limit for a category.</summary>
        public void SaveLimit(int categoryId, decimal monthlyLimit, int budgetStartDay = 1)
        {
            var existing = _db.BudgetLimits.FirstOrDefault(b => b.CategoryId == categoryId);
            if (existing == null)
            {
                _db.BudgetLimits.Add(new BudgetLimit
                {
                    CategoryId = categoryId,
                    MonthlyLimit = monthlyLimit,
                    BudgetStartDay = budgetStartDay
                });
            }
            else
            {
                existing.MonthlyLimit = monthlyLimit;
                existing.BudgetStartDay = budgetStartDay;
            }
            _db.SaveChanges();
        }

        /// <summary>Removes the budget limit for a category.</summary>
        public void DeleteLimit(int categoryId)
        {
            var existing = _db.BudgetLimits.FirstOrDefault(b => b.CategoryId == categoryId);
            if (existing != null)
            {
                _db.BudgetLimits.Remove(existing);
                _db.SaveChanges();
            }
        }

        // ── Spending Calculations ─────────────────────────────────────────────

        /// <summary>
        /// Returns the total spending for a given category in a given calendar month.
        /// </summary>
        public decimal GetMonthlySpending(int categoryId, int year, int month)
        {
            return _db.Transactions
                      .Where(t => t.CategoryId == categoryId
                               && t.Date.Year == year
                               && t.Date.Month == month)
                      .Sum(t => (decimal?)t.Amount) ?? 0m;
        }

        /// <summary>
        /// Returns the total spending for the same calendar month in the previous year.
        /// </summary>
        public decimal GetSameMonthLastYearSpending(int categoryId, int year, int month)
        {
            return GetMonthlySpending(categoryId, year - 1, month);
        }

        /// <summary>
        /// Builds a full summary for a single budget limit in a given month/year.
        /// </summary>
        public BudgetSummary GetSummary(BudgetLimit limit, int year, int month, double? cpiRate)
        {
            decimal spending = GetMonthlySpending(limit.CategoryId, year, month);
            decimal lastYear = GetSameMonthLastYearSpending(limit.CategoryId, year, month);

            decimal inflationAdjusted = cpiRate.HasValue && lastYear > 0
                ? lastYear * (1 + (decimal)(cpiRate.Value / 100.0))
                : lastYear;

            double? realDiffPct = null;
            if (inflationAdjusted > 0)
                realDiffPct = (double)((spending - inflationAdjusted) / inflationAdjusted * 100m);

            return new BudgetSummary
            {
                Budget = limit,
                Spending = spending,
                LastYearSpending = lastYear,
                InflationAdjustedLastYear = inflationAdjusted,
                CpiRate = cpiRate,
                RealDifferencePercent = realDiffPct
            };
        }
    }

    /// <summary>DTO holding all display data for one category's budget card.</summary>
    public class BudgetSummary
    {
        public BudgetLimit Budget { get; set; } = null!;

        /// <summary>Actual spending this month.</summary>
        public decimal Spending { get; set; }

        /// <summary>Spending in the same month last year (0 if no data).</summary>
        public decimal LastYearSpending { get; set; }

        /// <summary>Last year's spending multiplied by (1 + CPI%). Equals LastYearSpending when CPI unavailable.</summary>
        public decimal InflationAdjustedLastYear { get; set; }

        /// <summary>Annual CPI rate used (null if unavailable).</summary>
        public double? CpiRate { get; set; }

        /// <summary>
        /// Percent difference of this year's spending vs inflation-adjusted last year.
        /// Negative = spending less in real terms (good), Positive = spending more (potential concern).
        /// </summary>
        public double? RealDifferencePercent { get; set; }

        /// <summary>Progress ratio: spending / limit (capped at 1.2 for display).</summary>
        public double ProgressRatio => Budget.MonthlyLimit > 0
            ? Math.Min((double)(Spending / Budget.MonthlyLimit), 1.2)
            : 0;

        /// <summary>Status based on spending vs limit.</summary>
        public BudgetStatus Status
        {
            get
            {
                if (Budget.MonthlyLimit <= 0) return BudgetStatus.NoLimit;
                double pct = (double)(Spending / Budget.MonthlyLimit);
                if (pct >= 1.0) return BudgetStatus.Exceeded;
                if (pct >= 0.75) return BudgetStatus.Warning;
                return BudgetStatus.Good;
            }
        }
    }

    public enum BudgetStatus { NoLimit, Good, Warning, Exceeded }
}
