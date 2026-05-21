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
            
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                    .ThenInclude(c => c!.ParentCategory)
                .Include(t => t.CreditCardAccount)
                .Where(t => t.Date.Year == selectedYear && t.Date.Month == selectedMonth)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            var incomeList = transactions.Where(t => t.Category?.Type == TransactionType.Income).ToList();
            var expenseList = transactions.Where(t => t.Category?.Type == TransactionType.Expense).ToList();
            var transferList = transactions.Where(t => t.Category?.Type != TransactionType.Income && t.Category?.Type != TransactionType.Expense).ToList();

            // Using UI thread safely (Avalonia handles collection updates if bound correctly, but Dispatcher is safer. 
            // We'll trust ObservableCollection for now as this usually runs on UI thread due to the constructor call, but we might need Avalonia.Threading.Dispatcher)
            global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
                IncomeList.Clear();
                foreach (var t in incomeList) IncomeList.Add(t);

                ExpenseList.Clear();
                foreach (var t in expenseList) ExpenseList.Add(t);

                TransferList.Clear();
                foreach (var t in transferList) TransferList.Add(t);

                TotalIncome = incomeList.Sum(t => t.Amount);
                TotalExpense = expenseList.Sum(t => t.Amount);
                CurrentBalanceDisplay = "₺0,00"; // Placeholder for wealth
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
}
