using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Data;
using FinTrack.WPF.Helpers;

namespace FinTrack.WPF.Views
{
    public partial class SellInvestmentWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly int _assetId;
        private InvestmentAsset? _asset;

        public SellInvestmentWindow(AppDbContext context, int assetId)
        {
            InitializeComponent();
            _context = context;
            _assetId = assetId;
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _asset = await _context.InvestmentAssets.FindAsync(_assetId);
                if (_asset == null)
                {
                    MessageBox.Show("Varlık bulunamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                AssetInfoText.Text = $"Varlık: {_asset.Name} ({_asset.Symbol})";
                AvailableAmountText.Text = $"Satılabilir Miktar: {_asset.TotalAmount:N2}";

                var accounts = await _context.BankAccounts
                    .OrderBy(a => a.BankName)
                    .ThenBy(a => a.AccountName)
                    .ToListAsync();

                var allBankTransactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.BankAccountId != null)
                    .ToListAsync();
                    
                var allInvestmentTransactions = await _context.InvestmentTransactions
                    .Where(t => t.LinkedBankAccountId != null)
                    .ToListAsync();

                var items = new System.Collections.Generic.List<object>();
                items.Add(new { Id = "0", DisplayInfo = "-- Seçiniz (Opsiyonel) --", Balance = 0m });

                foreach (var account in accounts)
                {
                    decimal currentBalance = account.InitialBalance;

                    foreach (var t in allBankTransactions.Where(x => x.BankAccountId == account.Id))
                    {
                        if (t.Category?.Type == TransactionType.Income) currentBalance += t.Amount;
                        else if (t.Category?.Type == TransactionType.Expense) currentBalance -= t.Amount;
                        else if (t.Category?.Type == TransactionType.Transfer) currentBalance += t.Amount;
                    }

                    foreach (var invT in allInvestmentTransactions.Where(x => x.LinkedBankAccountId == account.Id))
                    {
                        if (invT.Type == InvestmentTransactionType.Buy) currentBalance -= invT.TotalCost;
                        else if (invT.Type == InvestmentTransactionType.Sell) currentBalance += invT.TotalCost;
                    }

                    items.Add(new { Id = "B_" + account.Id, DisplayInfo = $"🏦 {account.BankName} - {account.AccountName} ({currentBalance:C2})", Balance = currentBalance });
                }

                // Kredi Kartları
                var creditCards = await _context.CreditCardAccounts
                    .OrderBy(c => c.BankName)
                    .ThenBy(c => c.CardLabel)
                    .ToListAsync();
                    
                var allCcTransactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.CreditCardAccountId != null)
                    .ToListAsync();

                foreach (var card in creditCards)
                {
                    decimal currentDebt = 0; 
                    
                    foreach (var t in allCcTransactions.Where(x => x.CreditCardAccountId == card.Id))
                    {
                        if (t.Category?.Type == TransactionType.Expense) currentDebt += t.Amount;
                        else if (t.Category?.Type == TransactionType.Income || t.Category?.Type == TransactionType.Transfer) currentDebt -= t.Amount;
                    }
                    
                    foreach (var invT in allInvestmentTransactions.Where(x => x.LinkedCreditCardAccountId == card.Id))
                    {
                        if (invT.Type == InvestmentTransactionType.Buy) currentDebt += invT.TotalCost;
                        else if (invT.Type == InvestmentTransactionType.Sell) currentDebt -= invT.TotalCost;
                    }

                    items.Add(new { Id = "C_" + card.Id, DisplayInfo = $"💳 {card.BankName} - {card.CardLabel} (Güncel Borç: {currentDebt:C2})", Balance = currentDebt });
                }

                BankAccountComboBox.ItemsSource = items;
                BankAccountComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veriler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NumberTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UIHelper.FormatAmountTextBox(sender as System.Windows.Controls.TextBox);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_asset == null) return;

                if (!UIHelper.TryParseAmount(AmountTextBox.Text, out decimal sellAmount) || sellAmount <= 0)
                {
                    MessageBox.Show("Lütfen geçerli bir miktar giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (sellAmount > _asset.TotalAmount)
                {
                    MessageBox.Show("Satılacak miktar, sahip olduğunuz miktardan fazla olamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!UIHelper.TryParseAmount(UnitPriceTextBox.Text, out decimal unitPrice) || unitPrice < 0)
                {
                    MessageBox.Show("Lütfen geçerli bir satış fiyatı giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!UIHelper.TryParseAmount(FeeTextBox.Text, out decimal fee) || fee < 0)
                {
                    MessageBox.Show("Lütfen geçerli bir masraf/komisyon tutarı giriniz (yoksa 0 yazınız).", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal totalRevenue = (sellAmount * unitPrice) - fee;
                int? bankAccountId = null;
                int? creditCardAccountId = null;

                string? sourceId = BankAccountComboBox.SelectedValue?.ToString();
                if (!string.IsNullOrEmpty(sourceId) && sourceId != "0")
                {
                    if (sourceId.StartsWith("B_")) bankAccountId = int.Parse(sourceId.Substring(2));
                    else if (sourceId.StartsWith("C_")) creditCardAccountId = int.Parse(sourceId.Substring(2));
                }

                // Banka hesabı bakiyesi sadece raporlama/görüntüleme anında _context üzerinden hesaplandığı için
                // burada BankAccount üzerinde herhangi bir Balance alanı güncellememize gerek yok.
                // investmentTransactions tarafına LinkedBankAccountId'yi eklememiz yeterli. Hesaplama yapıldığında bakiye artacaktır.

                // 2. Varlığı güncelle veya tamamen sil (hepsini sattıysa)
                if (sellAmount == _asset.TotalAmount)
                {
                    // Tamamı satıldıysa isteğe bağlı olarak kaydı silebiliriz ya da miktarını 0 diyip bırakabiliriz.
                    // İşlem geçmişi tutmak istiyorsak miktarını 0 yaparak tutmak daha mantıklıdır.
                    _asset.TotalAmount = 0;
                    _asset.AverageCost = 0; // Cost basis evaporates
                }
                else
                {
                    _asset.TotalAmount -= sellAmount;
                    // Kalan miktar için ortalama maliyet (AverageCost) aynı kalır!
                }

                _context.InvestmentAssets.Update(_asset);

                // 3. İşlem (Transaction) Kaydı
                var transaction = new InvestmentTransaction
                {
                    InvestmentAsset = _asset,
                    Type = InvestmentTransactionType.Sell,
                    Amount = sellAmount,
                    UnitPrice = unitPrice,
                    Fee = fee,
                    TotalCost = totalRevenue, // Burada TotalCost = Satıştan elde edilen gelir anlamında
                    Date = DateTime.Now,
                    Notes = $"Varlık satışı: {sellAmount} {_asset.Symbol} @ {unitPrice:C2} (Masraf: {fee:C2})",
                    LinkedBankAccountId = bankAccountId,
                    LinkedCreditCardAccountId = creditCardAccountId
                };

                _context.InvestmentTransactions.Add(transaction);
                
                await _context.SaveChangesAsync();

                MessageBox.Show("Satış başarıyla gerçekleştirildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Satış hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
