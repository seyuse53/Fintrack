using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FinTrack.Core.Models;
using FinTrack.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.Extensions.DependencyInjection;
using FinTrack.Avalonia.Localization;
using FinTrack.Core.Helpers;
using Avalonia.Platform.Storage;

namespace FinTrack.Avalonia.ViewModels;

public class CardItemViewModel
{
    public int CardId { get; set; }
    public required string BankName { get; set; }
    public required string CardLabel { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal Limit { get; set; }
    public decimal RemainingLimit { get; set; }
    public double LimitProgressValue { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public bool CanPay { get; set; }
    
    public string StatementDescription { get; set; } = string.Empty;
    public string DaysRemainingText { get; set; } = string.Empty;
    public string LinkedCardsText { get; set; } = string.Empty;
    public double ProgressValue { get; set; }
    public string ProgressColor { get; set; } = "#3498DB";
}

public partial class CardsViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;

    [ObservableProperty]
    private ObservableCollection<CardItemViewModel> _cardsList = new();

    [ObservableProperty]
    private decimal _totalCreditLimit;

    [ObservableProperty]
    private decimal _totalCreditDebt;

    [ObservableProperty]
    private bool _isLoading;

    public CardsViewModel()
    {
        _context = AppDbContext.CreateNew();
    }

    public override void Dispose()
    {
        base.Dispose();
        _context?.Dispose();
    }

    public async Task LoadDataAsync()
    {
        if (_context == null) return;
        
        try
        {
            var now = DateTime.Now;

            // --- FIX FOR INCORRECT CREDIT CARD DEBT CATEGORY ---
            var badBalancesDb = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Category != null && t.Category.Type == TransactionType.Income)
                .ToListAsync();

            var badBalances = badBalancesDb
                .Where(t => t.Description != null && t.Description.Contains("Geçmiş Borç Dengelemesi"))
                .ToList();

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
            
            // --- FIX FOR KREDİ KARTI NAKİT ÇEKİM AND EKSTRE ÖDEMESİ ---
            var categoryFixes = await _context.Categories
                .Where(c => (c.Name == "Kredi Kartı Çekilen" || c.Name == "Kredi Kartı Nakit Çekim" || c.Name == "Ekstra Ödemesi" || c.Name == "Ekstre Ödemesi"))
                .ToListAsync();

            bool changedCat = false;
            foreach (var c in categoryFixes)
            {
                if ((c.Name == "Kredi Kartı Çekilen" || c.Name == "Kredi Kartı Nakit Çekim") && c.Type != TransactionType.Expense)
                {
                    c.Name = "Kredi Kartı Nakit Çekim";
                    c.Type = TransactionType.Expense;
                    changedCat = true;
                }
                if ((c.Name == "Ekstra Ödemesi" || c.Name == "Ekstre Ödemesi") && c.Type != TransactionType.Transfer)
                {
                    c.Name = "Ekstre Ödemesi";
                    c.Type = TransactionType.Transfer;
                    changedCat = true;
                }
            }
            if (changedCat)
            {
                await _context.SaveChangesAsync();
            }
            // --------------------------------------------------
            
            // --- FIX FOR AÇILIŞ BAKİYESİ ON CREDIT CARDS ---
            var openingBalanceCat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Açılış Bakiyesi");
            if (openingBalanceCat != null)
            {
                var badOpeningTx = await _context.Transactions
                    .Where(t => t.CreditCardAccountId != null && t.CategoryId == openingBalanceCat.Id)
                    .ToListAsync();

                if (badOpeningTx.Any())
                {
                    var openingDebtCat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Açılış Borcu");
                    if (openingDebtCat == null)
                    {
                        openingDebtCat = new Category { Name = "Açılış Borcu", Type = TransactionType.Expense };
                        _context.Categories.Add(openingDebtCat);
                        await _context.SaveChangesAsync();
                    }

                    foreach (var t in badOpeningTx)
                    {
                        t.CategoryId = openingDebtCat.Id;
                    }
                    await _context.SaveChangesAsync();
                }
            }
            // --------------------------------------------------
            
            var activeCards = await _context.CreditCardAccounts
                .Where(c => c.IsActive)
                .OrderBy(c => c.BankName)
                .ThenBy(c => c.CardLabel)
                .ToListAsync();

            var cardTransactions = await _context.Transactions
                .Include(t => t.CreditCardAccount)
                .Include(t => t.Category)
                .Where(t => t.CreditCardAccount != null && t.CreditCardAccount.IsActive)
                .ToListAsync();

            var masterCards = activeCards.Where(c => c.ParentCardId == null).ToList();
            var childCards = activeCards.Where(c => c.ParentCardId != null).ToList();

            var newItems = new System.Collections.Generic.List<CardItemViewModel>();
            
            decimal totalLimit = 0;
            decimal totalDebt = 0;

            foreach (var master in masterCards)
            {
                var familyIds = new System.Collections.Generic.List<int> { master.Id };
                var linkedOnes = childCards.Where(c => c.ParentCardId == master.Id).ToList();
                familyIds.AddRange(linkedOnes.Select(c => c.Id));

                var familyTransactions = cardTransactions.Where(t => familyIds.Contains(t.CreditCardAccountId ?? 0));
                var period = master.GetStatementPeriod(now);
                
                decimal statementDebt = 0;
                decimal consolidatedDebt = 0;
                
                var debugLines = new System.Collections.Generic.List<string>();
                debugLines.Add($"Card Family: {master.CardLabel} ({master.Id})");

                foreach (var t in familyTransactions)
                {
                    decimal amount = 0;
                    if (t.Category?.Type == TransactionType.Expense)
                        amount = t.Amount;
                    else if (t.Category?.Type == TransactionType.Income || t.Category?.Type == TransactionType.Transfer)
                        amount = -t.Amount;

                    consolidatedDebt += amount;

                    if (t.Date <= period.End)
                    {
                        statementDebt += amount;
                    }
                    
                    debugLines.Add($"Tx {t.Id}: Date={t.Date:d}, Cat='{t.Category?.Name}' ({t.Category?.Type}), Amount={t.Amount}, Effective={amount}");
                }
                
                System.IO.File.WriteAllLines($@"C:\VSRepos\FinTrack\LocalData\cc_debug_{master.Id}.txt", debugLines);

                totalLimit += master.Limit;
                totalDebt += statementDebt;

                var vm = new CardItemViewModel
                {
                    CardId = master.Id,
                    BankName = master.BankName,
                    CardLabel = master.CardLabel,
                    TotalDebt = statementDebt,
                    Limit = master.Limit,
                    RemainingLimit = master.Limit > 0 ? (master.Limit - consolidatedDebt) : 0,
                    LimitProgressValue = (master.Limit > 0) ? (double)(consolidatedDebt / master.Limit * 100) : 0,
                    PeriodStart = period.Start,
                    PeriodEnd = period.End,
                    CanPay = consolidatedDebt > 0,
                    LinkedCardsText = linkedOnes.Any() 
                        ? "+ " + string.Join(", ", linkedOnes.Select(o => o.CardLabel))
                        : string.Empty
                };

                double totalDays = (period.End - period.Start).TotalDays;
                double elapsedDays = (now - period.Start).TotalDays;
                double remainingDays = (period.End - now).TotalDays;

                if (remainingDays < 0) remainingDays = 0;
                if (elapsedDays < 0) elapsedDays = 0;
                if (totalDays <= 0) totalDays = 1;

                vm.ProgressValue = (elapsedDays / totalDays) * 100;

                if (remainingDays <= 3)
                {
                    vm.ProgressColor = "#E74C3C"; 
                    vm.DaysRemainingText = string.Format(LocalizationService.GetString("Cards_DaysLeftExcl"), Math.Ceiling(remainingDays));
                }
                else if (remainingDays <= 10)
                {
                    vm.ProgressColor = "#F39C12"; 
                    vm.DaysRemainingText = string.Format(LocalizationService.GetString("Cards_DaysLeft"), Math.Ceiling(remainingDays));
                }
                else
                {
                    vm.ProgressColor = "#3498DB"; 
                    vm.DaysRemainingText = string.Format(LocalizationService.GetString("Cards_Days"), Math.Ceiling(remainingDays));
                }

                vm.StatementDescription = string.Format(LocalizationService.GetString("Cards_StatementDesc"), period.Start.ToString("dd MMM"), period.End.ToString("dd MMM"));
                
                newItems.Add(vm);
            }

            global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
                TotalCreditLimit = totalLimit;
                TotalCreditDebt = totalDebt;
                CardsList.Clear();
                foreach(var item in newItems) CardsList.Add(item);
            });
        }
        catch(Exception ex)
        {
            AppLogger.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PayDebtAsync(int cardId)
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var window = new FinTrack.Avalonia.Views.PayCreditCardWindow(cardId);
            await window.ShowDialog(desktop.MainWindow);
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task ShowDetailsAsync(int cardId)
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var window = new FinTrack.Avalonia.Views.CardDetailWindow(cardId);
            await window.ShowDialog(desktop.MainWindow);
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task ManageCardsAsync()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var window = new FinTrack.Avalonia.Views.ManageCardsWindow();
            await window.ShowDialog(desktop.MainWindow);
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task UploadStatementAsync(int cardId)
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Kredi Kartı Ekstresi Seçin (PDF veya Görsel)",
                AllowMultiple = false,
                FileTypeFilter = new[] 
                { 
                    new FilePickerFileType("Desteklenen Dosyalar") { Patterns = new[] { "*.pdf", "*.png", "*.jpg", "*.jpeg" } } 
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                var filePath = file.Path.LocalPath;
                string apiKey = FinTrack.Core.Services.SettingsManager.GetGeminiApiKey() ?? string.Empty;
                
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    var errorDialog = new FinTrack.Avalonia.Views.ConfirmDialog("API Anahtarı Eksik", "Lütfen Ayarlar sayfasından Gemini API anahtarınızı giriniz.", "Tamam", "");
                    await errorDialog.ShowDialog<bool?>(desktop.MainWindow);
                    return;
                }

                try
                {
                    IsLoading = true;
                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    string extension = System.IO.Path.GetExtension(filePath);
                    
                    var parser = new FinTrack.Core.Services.GeminiParserService();
                    var parsedTransactions = await parser.ParseStatementAsync(fileBytes, extension, apiKey);

                    if (parsedTransactions == null || parsedTransactions.Count == 0)
                    {
                        var infoDialog = new FinTrack.Avalonia.Views.ConfirmDialog("Sonuç Bulunamadı", "Dosyadan herhangi bir işlem çıkarılamadı.", "Tamam", "");
                        await infoDialog.ShowDialog<bool?>(desktop.MainWindow);
                        return;
                    }

                    var window = new FinTrack.Avalonia.Views.ImportPreviewWindow(cardId, null, parsedTransactions);
                    await window.ShowDialog(desktop.MainWindow);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    var errorDialog = new FinTrack.Avalonia.Views.ConfirmDialog("Hata Oluştu", $"Ekstre işlenirken bir hata oluştu:\n{ex.Message}", "Tamam", "");
                    await errorDialog.ShowDialog<bool?>(desktop.MainWindow);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
    }
}
