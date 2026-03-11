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
    public partial class AddInvestmentWindow : Window
    {
        private readonly AppDbContext _context;

        public AddInvestmentWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            Loaded += async (s, e) => await LoadBankAccountsAsync();
        }

        private async Task LoadBankAccountsAsync()
        {
            try
            {
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

                    // Normal işlemler
                    foreach (var t in allBankTransactions.Where(x => x.BankAccountId == account.Id))
                    {
                        if (t.Category?.Type == TransactionType.Income) currentBalance += t.Amount;
                        else if (t.Category?.Type == TransactionType.Expense) currentBalance -= t.Amount;
                        else if (t.Category?.Type == TransactionType.Transfer) currentBalance += t.Amount;
                    }

                    // Yatırım işlemleri
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
                    decimal currentDebt = 0; // Kredi kartında bakiye hesabı
                    
                    foreach (var t in allCcTransactions.Where(x => x.CreditCardAccountId == card.Id))
                    {
                        if (t.Category?.Type == TransactionType.Expense) currentDebt += t.Amount;
                        else if (t.Category?.Type == TransactionType.Income || t.Category?.Type == TransactionType.Transfer) currentDebt -= t.Amount;
                    }
                    
                    // Yatırım işlemlerini ekle (alım yapınca borç artar, satış yapınca borç azalır)
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
                MessageBox.Show($"Banka hesapları yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
                string symbol = SymbolTextBox.Text.Trim().ToUpper();
                string name = NameTextBox.Text.Trim();
                string category = (CategoryComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Diğer";
                
                if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Lütfen sembol ve isim alanlarını doldurunuz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!UIHelper.TryParseAmount(AmountTextBox.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Lütfen geçerli bir miktar giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!UIHelper.TryParseAmount(UnitPriceTextBox.Text, out decimal unitPrice) || unitPrice < 0)
                {
                    MessageBox.Show("Lütfen geçerli bir birim fiyat giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!UIHelper.TryParseAmount(FeeTextBox.Text, out decimal fee) || fee < 0)
                {
                    MessageBox.Show("Lütfen geçerli bir masraf/komisyon tutarı giriniz (yoksa 0 yazınız).", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal totalCost = (amount * unitPrice) + fee;
                int? bankAccountId = null;
                int? creditCardAccountId = null;

                string? sourceId = BankAccountComboBox.SelectedValue?.ToString();
                if (!string.IsNullOrEmpty(sourceId) && sourceId != "0")
                {
                    if (sourceId.StartsWith("B_")) bankAccountId = int.Parse(sourceId.Substring(2));
                    else if (sourceId.StartsWith("C_")) creditCardAccountId = int.Parse(sourceId.Substring(2));
                }

                // 1. Ödeme kaynağından düşeceksek bakiyeyi/limiti kontrol edelim
                if (bankAccountId.HasValue || creditCardAccountId.HasValue)
                {
                    if (creditCardAccountId.HasValue)
                    {
                        // Use our new specialized limit check for credit cards
                        if (!UIHelper.CheckCardLimit(_context, creditCardAccountId.Value, totalCost, this))
                        {
                            return; // Aborted by user
                        }
                    }
                    else
                    {
                        // Keep existing logic for bank account balance check
                        var selectedItem = BankAccountComboBox.SelectedItem as dynamic;
                        if (selectedItem != null)
                        {
                            decimal currentBalance = (decimal)selectedItem.Balance;
                            
                            var propInfo = selectedItem.GetType().GetProperty("Balance");
                            if (propInfo != null)
                            {
                                currentBalance = (decimal)propInfo.GetValue(selectedItem);
                            }

                            if (currentBalance < totalCost)
                            {
                                var answer = MessageBox.Show($"Seçili hesaptaki bakiye yetersiz! ({currentBalance:C2} mevcut, {totalCost:C2} istenen)\n\nYine de işleme devam edilsin mi?", "Yetersiz Bakiye", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                if (answer == MessageBoxResult.No)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }

                // 2. Varlık var mı diye bakalım, yoksa yenisini yaratalım
                var asset = await _context.InvestmentAssets.FirstOrDefaultAsync(a => a.Symbol == symbol);
                if (asset == null)
                {
                    asset = new InvestmentAsset
                    {
                        Symbol = symbol,
                        Name = name,
                        Category = category,
                        TotalAmount = amount,
                        AverageCost = unitPrice
                    };
                    _context.InvestmentAssets.Add(asset);
                }
                else
                {
                    // Ortalama maliyeti yeniden hesapla ( (EskiToplamTutar + YeniToplamTutar) / YeniToplamMiktar )
                    decimal oldCostBasis = asset.TotalAmount * asset.AverageCost;
                    decimal newTotalAmount = asset.TotalAmount + amount;
                    decimal newTotalCostBasis = oldCostBasis + totalCost;
                    
                    asset.AverageCost = newTotalAmount > 0 ? (newTotalCostBasis / newTotalAmount) : 0;
                    asset.TotalAmount = newTotalAmount;
                    
                    // Kategori değişmiş olabilir, son eklenene göre güncelleyelim
                    asset.Category = category;
                    _context.InvestmentAssets.Update(asset);
                }

                // 3. İşlem (Transaction) Kaydını Oluştur
                var transaction = new InvestmentTransaction
                {
                    InvestmentAsset = asset,
                    Type = InvestmentTransactionType.Buy,
                    Amount = amount,
                    UnitPrice = unitPrice,
                    Fee = fee,
                    TotalCost = totalCost,
                    Date = DateTime.Now,
                    Notes = $"Varlık alımı: {amount} {symbol} @ {unitPrice:C2} (Masraf: {fee:C2})",
                    LinkedBankAccountId = bankAccountId,
                    LinkedCreditCardAccountId = creditCardAccountId
                };

                _context.InvestmentTransactions.Add(transaction);

                // Ortak transaction tablosuna da "Gider" olarak kaydetmek istenebilir (Opsiyonel)
                // Şimdilik sadece banka bakiyesini düşürüp kendi tablosunda tutuyoruz ki çift gider yazılmasın.
                
                await _context.SaveChangesAsync();

                var infoDialog = new FinTrack.WPF.Views.InfoDialogWindow("Başarılı", "İşlem başarıyla kaydedildi.");
                infoDialog.Owner = Window.GetWindow(this);
                infoDialog.ShowDialog();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
