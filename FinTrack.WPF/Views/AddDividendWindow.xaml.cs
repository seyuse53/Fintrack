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
    public partial class AddDividendWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly int _assetId;

        public AddDividendWindow(AppDbContext context, int assetId)
        {
            InitializeComponent();
            _context = context;
            _assetId = assetId;
            DividendDatePicker.SelectedDate = DateTime.Now;
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var asset = await _context.InvestmentAssets.FindAsync(_assetId);
                if (asset == null)
                {
                    MessageBox.Show("Varlık bulunamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                AssetInfoText.Text = $"Varlık: {asset.Name} ({asset.Symbol}) — {asset.TotalAmount:N2} adet";

                // Hesap listesini yükle
                var accounts = await _context.BankAccounts
                    .OrderBy(a => a.BankName)
                    .ThenBy(a => a.AccountName)
                    .ToListAsync();

                var items = new System.Collections.Generic.List<object>();
                items.Add(new { Id = "0", DisplayInfo = "-- Seçiniz (Opsiyonel) --" });

                foreach (var account in accounts)
                {
                    items.Add(new { Id = "B_" + account.Id, DisplayInfo = $"🏦 {account.BankName} - {account.AccountName}" });
                }

                BankAccountComboBox.ItemsSource = items;
                BankAccountComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veriler yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Dividend_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UIHelper.FormatAmountTextBox(sender as System.Windows.Controls.TextBox);
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            try
            {
                UIHelper.TryParseAmount(GrossDividendTextBox?.Text ?? "", out decimal gross);
                UIHelper.TryParseAmount(WithholdingRateTextBox?.Text ?? "", out decimal rate);

                decimal withholding = gross * (rate / 100m);
                decimal net = gross - withholding;

                if (WithholdingAmountText != null)
                    WithholdingAmountText.Text = $"₺{withholding:N2}";
                if (NetDividendPreviewText != null)
                    NetDividendPreviewText.Text = $"₺{net:N2}";
            }
            catch { /* Ignore */ }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var asset = await _context.InvestmentAssets.FindAsync(_assetId);
                if (asset == null)
                {
                    MessageBox.Show("Varlık bulunamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!UIHelper.TryParseAmount(GrossDividendTextBox.Text, out decimal grossDividend) || grossDividend <= 0)
                {
                    MessageBox.Show("Lütfen geçerli bir brüt temettü tutarı giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!UIHelper.TryParseAmount(WithholdingRateTextBox.Text, out decimal withholdingRate))
                {
                    withholdingRate = 10; // Varsayılan %10
                }

                decimal withholdingAmount = grossDividend * (withholdingRate / 100m);
                decimal netDividend = grossDividend - withholdingAmount;
                DateTime dividendDate = DividendDatePicker.SelectedDate ?? DateTime.Now;

                int? bankAccountId = null;
                string? sourceId = BankAccountComboBox.SelectedValue?.ToString();
                if (!string.IsNullOrEmpty(sourceId) && sourceId != "0" && sourceId.StartsWith("B_"))
                {
                    bankAccountId = int.Parse(sourceId.Substring(2));
                }

                // Temettü işlemi oluştur
                var transaction = new InvestmentTransaction
                {
                    InvestmentAsset = asset,
                    Type = InvestmentTransactionType.Dividend,
                    Amount = asset.TotalAmount, // Temettü anında sahip olunan adet
                    UnitPrice = grossDividend / asset.TotalAmount, // Adet başına brüt temettü
                    Fee = withholdingAmount, // Stopaj, komisyon alanına yazılır
                    TotalCost = netDividend, // Net temettü geliri
                    Date = dividendDate,
                    Notes = $"Temettü geliri: Brüt ₺{grossDividend:N2}, Stopaj %{withholdingRate:N1} (₺{withholdingAmount:N2}), Net ₺{netDividend:N2}",
                    LinkedBankAccountId = bankAccountId
                };

                _context.InvestmentTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                var infoDialog = new InfoDialogWindow("Başarılı", $"Temettü geliri kaydedildi.\nNet: ₺{netDividend:N2}");
                infoDialog.Owner = this;
                infoDialog.ShowDialog();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Temettü kayıt hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
