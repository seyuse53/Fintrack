using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using FinTrack.Core.Models;
using FinTrack.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Collections.Generic;
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
            // Seed Point Categories if they don't exist
            if (!await _context.Categories.AnyAsync(c => c.Name == "Puan Kullanımı"))
            {
                _context.Categories.Add(new Category { Name = "Puan Kullanımı", Type = TransactionType.Income, IsVisible = true });
                await _context.SaveChangesAsync();
            }

            // Temizlik: Puan Kazanımı eklendiyse kaldıralım (kullanıcı istemedi)
            var kazanimi = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Puan Kazanımı");
            if (kazanimi != null)
            {
                _context.Categories.Remove(kazanimi);
                await _context.SaveChangesAsync();
            }

            // --- FIX FOR INCORRECT CREDIT CARD DEBT CATEGORY ---
            var badBalances = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Description != null && t.Description.Contains("Geçmiş Borç Dengelemesi") && t.Category != null && t.Category.Type == TransactionType.Income)
                .ToListAsync();

            if (badBalances.Any())
            {
                var expenseCat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Geçmiş Kredi Kartı Borcu");
                if (expenseCat == null) 
                {
                    expenseCat = new Category { Name = "Geçmiş Kredi Kartı Borcu", Type = TransactionType.Expense };
                    _context.Categories.Add(expenseCat);
                    await _context.SaveChangesAsync();
                }
                
                foreach(var b in badBalances)
                {
                    b.CategoryId = expenseCat.Id;
                }
                await _context.SaveChangesAsync();
            }
            // --------------------------------------------------

            // Excel AHE HATUN & AHE BENIM Seeder
            var aheHatun = await _context.InvestmentAssets.FirstOrDefaultAsync(a => a.Name.Contains("AHE HATUN"));
            var aheBenim = await _context.InvestmentAssets.FirstOrDefaultAsync(a => a.Name.Contains("AHE BENIM"));
            var troyCard = await _context.CreditCardAccounts.FirstOrDefaultAsync(c => c.CardLabel.Contains("Troy") && c.BankName.Contains("İş"));

            if (aheHatun != null && !await _context.InvestmentTransactions.AnyAsync(t => t.InvestmentAssetId == aheHatun.Id && t.GramGoldEquivalent != null))
            {
                var oldTrans = await _context.InvestmentTransactions.Where(t => t.InvestmentAssetId == aheHatun.Id).ToListAsync();
                _context.InvestmentTransactions.RemoveRange(oldTrans);

                var hatunDates = new List<(DateTime Date, decimal Amount, decimal GramGoldPrice)>
                {
                    (new DateTime(2022, 4, 4), 1000m, 911.24m), (new DateTime(2022, 4, 27), 500m, 897.96m),
                    (new DateTime(2022, 5, 20), 500m, 941.68m), (new DateTime(2022, 6, 7), 500m, 1000.10m),
                    (new DateTime(2022, 7, 13), 500m, 970.78m), (new DateTime(2022, 8, 2), 500m, 1021.72m),
                    (new DateTime(2022, 8, 31), 500m, 1001.64m), (new DateTime(2022, 9, 30), 1000m, 988.64m),
                    (new DateTime(2022, 10, 24), 1000m, 985.44m), (new DateTime(2022, 11, 24), 1000m, 1043.96m),
                    (new DateTime(2022, 12, 25), 500m, 1077.98m), (new DateTime(2023, 1, 25), 1640m, 1173.03m),
                    (new DateTime(2023, 3, 24), 1640m, 1214.44m), (new DateTime(2023, 4, 24), 1640m, 1240.82m),
                    (new DateTime(2023, 5, 25), 820m, 1247.84m), (new DateTime(2023, 6, 26), 1640m, 1605.59m),
                    (new DateTime(2023, 7, 24), 1640m, 1699.75m), (new DateTime(2023, 8, 25), 1640m, 1591.71m),
                    (new DateTime(2023, 9, 25), 1640m, 1676.52m), (new DateTime(2023, 10, 24), 820m, 1785.71m),
                    (new DateTime(2023, 11, 24), 1640m, 1856.54m), (new DateTime(2023, 12, 24), 1640m, 1924.83m),
                    
                    // 2024
                    (new DateTime(2024, 1, 24), 2000m, 1960.55m), (new DateTime(2024, 2, 28), 2000m, 2035.77m),
                    (new DateTime(2024, 3, 28), 2000m, 2302.68m), (new DateTime(2024, 4, 28), 4000m, 2441.97m),
                    (new DateTime(2024, 5, 28), 2000m, 2441.33m), (new DateTime(2024, 6, 28), 2000m, 2461.23m),
                    (new DateTime(2024, 7, 28), 3000m, 2526.96m), (new DateTime(2024, 8, 28), 4000m, 2745.12m),
                    (new DateTime(2024, 9, 27), 4000m, 2905.46m), (new DateTime(2024, 10, 27), 4000m, 3021.50m),
                    (new DateTime(2024, 11, 27), 4000m, 2941.22m), (new DateTime(2024, 12, 27), 4000m, 2964.79m),
                    
                    // 2025
                    (new DateTime(2025, 1, 27), 6000m, 3132.69m), (new DateTime(2025, 2, 27), 6000m, 3379.68m),
                    (new DateTime(2025, 3, 27), 6000m, 3740.91m), (new DateTime(2025, 4, 27), 6000m, 4054.95m),
                    (new DateTime(2025, 5, 27), 6000m, 4142.98m), (new DateTime(2025, 6, 27), 6000m, 4194.74m),
                    (new DateTime(2025, 7, 27), 6000m, 4345.54m), (new DateTime(2025, 8, 27), 7000m, 4493.56m),
                    (new DateTime(2025, 9, 27), 7000m, 5044.98m), (new DateTime(2025, 10, 27), 7000m, 5402.88m),
                    (new DateTime(2025, 11, 27), 14000m, 5683.64m), (new DateTime(2025, 12, 27), 7000m, 6240.21m),

                    // 2026
                    (new DateTime(2026, 1, 27), 8500m, 7085.23m), (new DateTime(2026, 2, 27), 8500m, 7391.74m),
                    (new DateTime(2026, 3, 27), 8500m, 6420.32m), (new DateTime(2026, 4, 27), 8500m, 6768.77m)
                };

                foreach (var item in hatunDates)
                {
                    _context.InvestmentTransactions.Add(new InvestmentTransaction
                    {
                        InvestmentAssetId = aheHatun.Id, Type = InvestmentTransactionType.Buy,
                        Amount = 1, UnitPrice = item.Amount, TotalCost = item.Amount,
                        Date = item.Date, LinkedCreditCardAccountId = troyCard?.Id,
                        GramGoldEquivalent = Math.Round(item.Amount / item.GramGoldPrice, 2),
                        Notes = "AHE HATUN Excel'den otomatik aktarıldı."
                    });
                }
                
                aheHatun.TotalAmount = hatunDates.Count;
                aheHatun.AverageCost = 178900m / hatunDates.Count;
                _context.InvestmentAssets.Update(aheHatun);

                if (troyCard != null)
                {
                    var oldBalancing = await _context.Transactions.Where(t => t.Description != null && t.Description.Contains("AHE HATUN Geçmiş")).ToListAsync();
                    if (oldBalancing.Any()) {
                        _context.Transactions.RemoveRange(oldBalancing);
                    }
                }
            }

            if (aheBenim != null && !await _context.InvestmentTransactions.AnyAsync(t => t.InvestmentAssetId == aheBenim.Id && t.GramGoldEquivalent != null))
            {
                var oldTrans = await _context.InvestmentTransactions.Where(t => t.InvestmentAssetId == aheBenim.Id).ToListAsync();
                _context.InvestmentTransactions.RemoveRange(oldTrans);

                var benimDates = new List<(DateTime Date, decimal Amount, decimal GramGoldPrice)>
                {
                    (new DateTime(2023, 4, 24), 1200m, 1240.82m), (new DateTime(2023, 5, 28), 600m, 1249.41m),
                    (new DateTime(2023, 6, 26), 600m, 1605.59m), (new DateTime(2023, 7, 24), 600m, 1699.75m),
                    (new DateTime(2023, 8, 25), 1200m, 1591.71m), (new DateTime(2023, 9, 25), 1200m, 1676.52m),
                    (new DateTime(2023, 10, 27), 1800m, 1800.84m), (new DateTime(2023, 11, 24), 1800m, 1856.54m),
                    (new DateTime(2023, 12, 24), 2400m, 1924.83m),

                    // 2024
                    (new DateTime(2024, 1, 24), 3000m, 1960.55m), (new DateTime(2024, 2, 28), 1000m, 2035.77m),
                    (new DateTime(2024, 3, 28), 1000m, 2302.68m), (new DateTime(2024, 4, 28), 1000m, 2441.97m),
                    (new DateTime(2024, 5, 28), 1000m, 2441.33m), (new DateTime(2024, 6, 28), 2000m, 2461.23m),
                    (new DateTime(2024, 7, 29), 1500m, 2526.50m), (new DateTime(2024, 8, 29), 2000m, 2767.19m),
                    (new DateTime(2024, 9, 27), 2000m, 2905.46m), (new DateTime(2024, 10, 27), 2000m, 3021.50m),
                    (new DateTime(2024, 11, 27), 2000m, 2941.22m), (new DateTime(2024, 12, 27), 2000m, 2964.79m),

                    // 2025
                    (new DateTime(2025, 1, 27), 4000m, 3132.69m), (new DateTime(2025, 2, 27), 4000m, 3379.68m),
                    (new DateTime(2025, 3, 27), 4000m, 3740.91m), (new DateTime(2025, 4, 27), 4000m, 4054.95m),
                    (new DateTime(2025, 5, 27), 4000m, 4142.98m), (new DateTime(2025, 6, 27), 4000m, 4194.74m),
                    (new DateTime(2025, 7, 27), 4000m, 4345.54m), (new DateTime(2025, 8, 27), 5000m, 4493.56m),
                    (new DateTime(2025, 9, 27), 5000m, 5044.98m), (new DateTime(2025, 10, 27), 5000m, 5402.88m),
                    (new DateTime(2025, 11, 27), 10000m, 5683.64m), (new DateTime(2025, 12, 27), 5000m, 6240.21m),

                    // 2026
                    (new DateTime(2026, 1, 27), 6500m, 7085.23m), (new DateTime(2026, 2, 27), 6500m, 7391.74m),
                    (new DateTime(2026, 3, 27), 6500m, 6420.32m), (new DateTime(2026, 4, 27), 6500m, 6768.77m)
                };

                foreach (var item in benimDates)
                {
                    _context.InvestmentTransactions.Add(new InvestmentTransaction
                    {
                        InvestmentAssetId = aheBenim.Id, Type = InvestmentTransactionType.Buy,
                        Amount = 1, UnitPrice = item.Amount, TotalCost = item.Amount,
                        Date = item.Date, LinkedCreditCardAccountId = troyCard?.Id,
                        GramGoldEquivalent = Math.Round(item.Amount / item.GramGoldPrice, 2),
                        Notes = "AHE BENİM Excel'den otomatik aktarıldı."
                    });
                }

                aheBenim.TotalAmount = benimDates.Count;
                aheBenim.AverageCost = 115900m / benimDates.Count;
                _context.InvestmentAssets.Update(aheBenim);

                if (troyCard != null)
                {
                    var oldBalancing = await _context.Transactions.Where(t => t.Description != null && t.Description.Contains("AHE BENİM Geçmiş")).ToListAsync();
                    if (oldBalancing.Any()) {
                        _context.Transactions.RemoveRange(oldBalancing);
                    }
                }
            }

            await _context.SaveChangesAsync();

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
            var investmentAssets = await _context.InvestmentAssets.Where(a => a.Category != "BES").ToListAsync();

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
