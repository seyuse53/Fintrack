using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FinTrack.Core.Models;
using FinTrack.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using FinTrack.Avalonia.Localization;

namespace FinTrack.Avalonia.ViewModels;

public partial class ManageCardsViewModel : ViewModelBase
{
    private readonly AppDbContext? _context;

    [ObservableProperty]
    private ObservableCollection<CreditCardAccount> _cards = new();

    [ObservableProperty]
    private ObservableCollection<CreditCardAccount> _availableParentCards = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableBanks = new(new[]
    {
        "Akbank", "Albaraka Türk", "Alternatif Bank", "Anadolubank", "BtcTurk", "Burgan Bank", 
        "DenizBank", "Enpara.com", "Fibabanka", "Garanti BBVA", "Halkbank", "HSBC", 
        "ING", "Kuveyt Türk", "Odeabank", "Papara", "QNB Finansbank", "Şekerbank", "TEB", 
        "Türkiye Finans", "Türkiye İş Bankası", "VakıfBank", "Vakıf Katılım", "Yapı Kredi", 
        "Ziraat Bankası", "Ziraat Katılım"
    });

    [ObservableProperty]
    private string _newBankName = string.Empty;

    [ObservableProperty]
    private string _newCardLabel = string.Empty;

    [ObservableProperty]
    private string _newLimitText = string.Empty;

    [ObservableProperty]
    private int _newStatementDay = 1;

    [ObservableProperty]
    private int _newPaymentDueDay = 15;

    [ObservableProperty]
    private CreditCardAccount? _selectedParentCard;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private bool _isParentNotSelected = true;

    [ObservableProperty]
    private bool _isEditing = false;

    [ObservableProperty]
    private string _formTitle = LocalizationService.GetString("ManageCards_AddTitle");

    [ObservableProperty]
    private string _submitButtonText = LocalizationService.GetString("ManageCards_AddButton");

    private CreditCardAccount? _editingCard = null;

    // Action to close window
    public Action? CloseAction { get; set; }

    partial void OnSelectedParentCardChanged(CreditCardAccount? value)
    {
        if (value != null && value.Id > 0)
        {
            IsParentNotSelected = false;
            NewBankName = value.BankName;
            NewLimitText = value.Limit > 0 ? value.Limit.ToString("G29", System.Globalization.CultureInfo.InvariantCulture) : "";
            NewStatementDay = value.StatementDay;
            NewPaymentDueDay = value.PaymentDueDay;
        }
        else
        {
            IsParentNotSelected = true;
        }
    }

    partial void OnNewStatementDayChanged(int value)
    {
        if (IsParentNotSelected)
        {
            int due = value + 10;
            if (due > 30) due -= 30;
            NewPaymentDueDay = due;
        }
    }

    public ManageCardsViewModel()
    {
        _context = AppDbContext.CreateNew();
        _ = LoadCardsAsync();
    }

    private async Task LoadCardsAsync()
    {
        if (_context == null) return;

        var dbCards = await _context.CreditCardAccounts
            .OrderBy(c => c.BankName)
            .ThenBy(c => c.CardLabel)
            .ToListAsync();

        var masterCards = dbCards.Where(c => c.ParentCardId == null).ToList();

        global::Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            Cards.Clear();
            foreach (var card in dbCards)
            {
                Cards.Add(card);
            }

            AvailableParentCards.Clear();
            // A card can be linked to another card (typically parent/master card)
            // Adding a dummy option for "None"
            AvailableParentCards.Add(new CreditCardAccount { Id = -1, BankName = "-", CardLabel = LocalizationService.GetString("ManageCards_NoLinkedCard") });
            
            foreach (var mc in masterCards)
                AvailableParentCards.Add(mc);

            SelectedParentCard = AvailableParentCards.First();
        });
    }

    [RelayCommand]
    private async Task AddCardAsync()
    {
        if (_context == null) return;

        if (string.IsNullOrWhiteSpace(NewBankName) || string.IsNullOrWhiteSpace(NewCardLabel))
        {
            ShowError(LocalizationService.GetString("ManageCards_ErrNameReq"));
            return;
        }

        decimal limit = 0;
        if (!string.IsNullOrWhiteSpace(NewLimitText))
        {
            string cleanLimit = NewLimitText.Replace(".", "").Replace(",", ".");
            if (!decimal.TryParse(cleanLimit, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out limit))
            {
                ShowError(LocalizationService.GetString("ManageCards_ErrInvalidLimit"));
                return;
            }
        }

        if (NewStatementDay < 1 || NewStatementDay > 31)
        {
            ShowError(LocalizationService.GetString("ManageCards_ErrStatementDay"));
            return;
        }

        if (NewPaymentDueDay < 1 || NewPaymentDueDay > 31)
        {
            ShowError(LocalizationService.GetString("ManageCards_ErrDueDay"));
            return;
        }

        if (IsEditing && _editingCard != null)
        {
            var dbCard = await _context.CreditCardAccounts.FindAsync(_editingCard.Id);
            if (dbCard != null)
            {
                if (SelectedParentCard?.Id > 0)
                {
                    dbCard.ParentCardId = SelectedParentCard.Id;
                    dbCard.BankName = SelectedParentCard.BankName;
                    dbCard.Limit = SelectedParentCard.Limit;
                    dbCard.StatementDay = SelectedParentCard.StatementDay;
                    dbCard.PaymentDueDay = SelectedParentCard.PaymentDueDay;
                    dbCard.CardLabel = NewCardLabel.Trim();
                }
                else
                {
                    dbCard.BankName = NewBankName.Trim();
                    dbCard.CardLabel = NewCardLabel.Trim();
                    dbCard.Limit = limit;
                    dbCard.StatementDay = NewStatementDay;
                    dbCard.PaymentDueDay = NewPaymentDueDay;
                    dbCard.ParentCardId = null;

                    var childCards = await _context.CreditCardAccounts.Where(c => c.ParentCardId == dbCard.Id).ToListAsync();
                    foreach (var child in childCards)
                    {
                        child.BankName = dbCard.BankName;
                        child.Limit = dbCard.Limit;
                        child.StatementDay = dbCard.StatementDay;
                        child.PaymentDueDay = dbCard.PaymentDueDay;
                    }
                }
            }
        }
        else
        {
            var newCard = new CreditCardAccount
            {
                CardLabel = NewCardLabel.Trim(),
                IsActive = true,
                ParentCardId = SelectedParentCard?.Id > 0 ? SelectedParentCard.Id : null,
                
                BankName = SelectedParentCard?.Id > 0 ? SelectedParentCard.BankName : NewBankName.Trim(),
                Limit = SelectedParentCard?.Id > 0 ? SelectedParentCard.Limit : limit,
                StatementDay = SelectedParentCard?.Id > 0 ? SelectedParentCard.StatementDay : NewStatementDay,
                PaymentDueDay = SelectedParentCard?.Id > 0 ? SelectedParentCard.PaymentDueDay : NewPaymentDueDay
            };

            _context.CreditCardAccounts.Add(newCard);
        }
        
        await _context.SaveChangesAsync();
        await LoadCardsAsync();

        CancelEdit();
    }

    [RelayCommand]
    private void EditCard(CreditCardAccount card)
    {
        _editingCard = card;
        IsEditing = true;
        FormTitle = LocalizationService.GetString("ManageCards_EditTitle");
        SubmitButtonText = LocalizationService.GetString("ManageAccounts_UpdateButton");

        NewBankName = card.BankName;
        NewCardLabel = card.CardLabel;
        NewLimitText = card.Limit > 0 ? card.Limit.ToString("G29", System.Globalization.CultureInfo.InvariantCulture) : "";
        NewStatementDay = card.StatementDay;
        NewPaymentDueDay = card.PaymentDueDay;
        
        SelectedParentCard = AvailableParentCards.FirstOrDefault(c => c.Id == card.ParentCardId);
        if (SelectedParentCard == null)
            SelectedParentCard = AvailableParentCards.First();
            
        ClearError();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _editingCard = null;
        IsEditing = false;
        FormTitle = LocalizationService.GetString("ManageCards_AddTitle");
        SubmitButtonText = LocalizationService.GetString("ManageCards_AddButton");

        NewBankName = string.Empty;
        NewCardLabel = string.Empty;
        NewLimitText = string.Empty;
        NewStatementDay = 1;
        NewPaymentDueDay = 15;
        SelectedParentCard = AvailableParentCards.First();
        ClearError();
    }

    [RelayCommand]
    private async Task ToggleActiveStatusAsync(CreditCardAccount card)
    {
        if (_context == null || card == null) return;

        var dbCard = await _context.CreditCardAccounts.FindAsync(card.Id);
        if (dbCard != null)
        {
            dbCard.IsActive = !dbCard.IsActive;
            await _context.SaveChangesAsync();
            await LoadCardsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteCardAsync(CreditCardAccount card)
    {
        if (_context == null || card == null) return;

        // Check if there are transactions
        bool hasTransactions = await _context.Transactions.AnyAsync(t => t.CreditCardAccountId == card.Id);
        if (hasTransactions)
        {
            ShowError(LocalizationService.GetString("ManageCards_ErrHasTx"));
            return;
        }

        // Check if it has child cards
        bool hasChildCards = await _context.CreditCardAccounts.AnyAsync(c => c.ParentCardId == card.Id);
        if (hasChildCards)
        {
            ShowError(LocalizationService.GetString("ManageCards_ErrHasChild"));
            return;
        }

        var dbCard = await _context.CreditCardAccounts.FindAsync(card.Id);
        if (dbCard != null)
        {
            _context.CreditCardAccounts.Remove(dbCard);
            await _context.SaveChangesAsync();
            await LoadCardsAsync();
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseAction?.Invoke();
    }

    private void ShowError(string msg)
    {
        ErrorMessage = msg;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
}
