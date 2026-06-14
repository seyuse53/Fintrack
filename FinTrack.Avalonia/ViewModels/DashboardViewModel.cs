using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using FinTrack.Core.Models;
using FinTrack.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Avalonia.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;

    [ObservableProperty]
    private decimal _totalIncome;

    [ObservableProperty]
    private decimal _totalExpense;

    [ObservableProperty]
    private string _currentBalanceDisplay = "₺0,00";

    [ObservableProperty]
    private decimal _investmentPortfolioValue;

    [ObservableProperty]
    private ObservableCollection<Transaction> _incomeList = new();

    [ObservableProperty]
    private ObservableCollection<Transaction> _expenseList = new();

    [ObservableProperty]
    private ObservableCollection<Transaction> _transferList = new();

    public DashboardViewModel()
    {
        _context = App.Services?.GetService<AppDbContext>();
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync(int? year = null, int? month = null)
    {
        if (_context == null) return;
        
        try
        {
            int selectedYear = year ?? DateTime.Now.Year;
            int selectedMonth = month ?? DateTime.Now.Month;
            
            // Calculate Previous Month
            int prevMonth = selectedMonth == 1 ? 12 : selectedMonth - 1;
            int prevYear = selectedMonth == 1 ? selectedYear - 1 : selectedYear;

            DateTime startDate = GetLastBusinessDayOfMonth(prevYear, prevMonth).Date;
            DateTime endDate = GetLastBusinessDayOfMonth(selectedYear, selectedMonth).Date;

            System.Diagnostics.Debug.WriteLine($"[Dashboard] Ay: {selectedMonth}/{selectedYear} | Aralık: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}");

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                    .ThenInclude(c => c!.ParentCategory)
                .Include(t => t.CreditCardAccount)
                .Where(t => t.Date >= startDate && t.Date < endDate)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"[Dashboard] Toplam işlem: {transactions.Count}");

            var incomeItems = transactions.Where(t => t.Category?.Type == TransactionType.Income).ToList();
            var expenseItems = transactions.Where(t => t.Category?.Type == TransactionType.Expense).ToList();
            var transferItems = transactions.Where(t => t.Category?.Type != TransactionType.Income && t.Category?.Type != TransactionType.Expense).ToList();

            System.Diagnostics.Debug.WriteLine($"[Dashboard] Gelir: {incomeItems.Count}, Gider: {expenseItems.Count}, Transfer: {transferItems.Count}");

            // Hesapla: Toplam Varlık (Nakit + Banka)
            var cashAndBankTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.CreditCardAccountId == null)
                .ToListAsync();

            decimal totalWealth = 0;
            foreach (var t in cashAndBankTransactions)
            {
                if (t.Category?.Type == TransactionType.Income || t.Category?.Type == TransactionType.Transfer)
                    totalWealth += t.Amount;
                else if (t.Category?.Type == TransactionType.Expense)
                    totalWealth -= t.Amount;
            }

            var allInvestmentTransactions = await _context.InvestmentTransactions
                .Where(t => t.LinkedBankAccountId != null)
                .ToListAsync();

            foreach (var invT in allInvestmentTransactions)
            {
                if (invT.Type == InvestmentTransactionType.Buy)
                    totalWealth -= invT.TotalCost;
                else if (invT.Type == InvestmentTransactionType.Sell)
                    totalWealth += invT.TotalCost;
            }

            global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
                IncomeList = new ObservableCollection<Transaction>(incomeItems);
                ExpenseList = new ObservableCollection<Transaction>(expenseItems);
                TransferList = new ObservableCollection<Transaction>(transferItems);

                TotalIncome = incomeItems.Sum(t => t.Amount);
                TotalExpense = expenseItems.Sum(t => t.Amount);
                CurrentBalanceDisplay = $"₺{totalWealth:N2}";
            });

            // Hesapla: Yatırım Portföyü
            var investmentAssets = await _context.InvestmentAssets.ToListAsync();

            decimal portfolioTotal = 0;
            foreach (var asset in investmentAssets)
            {
                decimal price = FinTrack.Core.Services.PricingService.GetCurrentPrice(asset.Symbol, asset.AverageCost);
                portfolioTotal += asset.TotalAmount * price;
            }

            global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
                InvestmentPortfolioValue = portfolioTotal;
            });
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    private DateTime GetLastBusinessDayOfMonth(int year, int month)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        DateTime lastDay = new DateTime(year, month, daysInMonth);
        while (lastDay.DayOfWeek == DayOfWeek.Saturday || lastDay.DayOfWeek == DayOfWeek.Sunday)
        {
            lastDay = lastDay.AddDays(-1);
        }
        return lastDay;
    }
}
