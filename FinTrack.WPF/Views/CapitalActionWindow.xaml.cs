using System;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Core.Models;
using FinTrack.Data;
using FinTrack.WPF.Helpers;

namespace FinTrack.WPF.Views
{
    public partial class CapitalActionWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly int _assetId;
        private InvestmentAsset? _asset;

        public CapitalActionWindow(AppDbContext context, int assetId)
        {
            InitializeComponent();
            _context = context;
            _assetId = assetId;
            ActionDatePicker.SelectedDate = DateTime.Now;
            Loaded += async (s, e) =>
            {
                _asset = await _context.InvestmentAssets.FindAsync(_assetId);
                if (_asset == null)
                {
                    MessageBox.Show("Varlık bulunamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }
                AssetInfoText.Text = $"{_asset.Symbol} — {_asset.Name}";
                CurrentAmountText.Text = $"{_asset.TotalAmount:N2}";
                CurrentAvgCostText.Text = $"₺{_asset.AverageCost:N2}";
                UpdatePreview();
            };
        }

        private void ActionType_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ActionTypeComboBox == null || RateLabel == null) return;

            int idx = ActionTypeComboBox.SelectedIndex;

            switch (idx)
            {
                case 0: // Bedelsiz
                    RateLabel.Text = "Bedelsiz Oranı (%)";
                    RateHelpText.Text = "Örnek: %50 bedelsiz → 100 lot ise 50 lot daha eklenir";
                    RateTextBox.Text = "50";
                    RightsIssuePricePanel.Visibility = Visibility.Collapsed;
                    DatePanel.Visibility = Visibility.Visible;
                    break;
                case 1: // Bedelli
                    RateLabel.Text = "Bedelli Oranı (%)";
                    RateHelpText.Text = "Örnek: %25 bedelli → 100 lot ise 25 lot rüçhan hakkı";
                    RateTextBox.Text = "25";
                    RightsIssuePricePanel.Visibility = Visibility.Visible;
                    DatePanel.Visibility = Visibility.Visible;
                    break;
                case 2: // Bölünme
                    RateLabel.Text = "Bölünme Çarpanı";
                    RateHelpText.Text = "Örnek: 2 yazarsanız → her 1 hisse, 2 hisseye bölünür";
                    RateTextBox.Text = "2";
                    RightsIssuePricePanel.Visibility = Visibility.Collapsed;
                    DatePanel.Visibility = Visibility.Visible;
                    break;
            }

            UpdatePreview();
        }

        private void Rate_Changed(object sender, TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_asset == null || NewAmountPreviewText == null) return;

            if (!decimal.TryParse(RateTextBox.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal rate) || rate <= 0)
            {
                NewAmountPreviewText.Text = "—";
                NewAvgCostPreviewText.Text = "—";
                PreviewDetailText.Text = "";
                return;
            }

            int actionType = ActionTypeComboBox?.SelectedIndex ?? 0;
            decimal currentAmount = _asset.TotalAmount;
            decimal currentAvgCost = _asset.AverageCost;
            decimal currentTotalCost = currentAmount * currentAvgCost;

            decimal newAmount, newAvgCost, addedShares;

            switch (actionType)
            {
                case 0: // Bedelsiz
                    addedShares = currentAmount * (rate / 100m);
                    newAmount = currentAmount + addedShares;
                    newAvgCost = newAmount > 0 ? currentTotalCost / newAmount : 0;
                    NewAmountPreviewText.Text = $"{newAmount:N2}";
                    NewAvgCostPreviewText.Text = $"₺{newAvgCost:N2}";
                    PreviewDetailText.Text = $"+{addedShares:N2} adet bedelsiz hisse eklenecek. Toplam maliyet değişmez, birim maliyet düşer.";
                    break;

                case 1: // Bedelli
                    addedShares = currentAmount * (rate / 100m);
                    decimal rightsPrice = 0;
                    decimal.TryParse(RightsPriceTextBox?.Text?.Replace(",", ".") ?? "0",
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out rightsPrice);

                    decimal rightsCost = addedShares * rightsPrice;
                    newAmount = currentAmount + addedShares;
                    newAvgCost = newAmount > 0 ? (currentTotalCost + rightsCost) / newAmount : 0;
                    NewAmountPreviewText.Text = $"{newAmount:N2}";
                    NewAvgCostPreviewText.Text = $"₺{newAvgCost:N2}";
                    PreviewDetailText.Text = $"+{addedShares:N2} adet bedelli hisse @ ₺{rightsPrice:N2} = ₺{rightsCost:N2} ek maliyet.";
                    break;

                case 2: // Bölünme
                    newAmount = currentAmount * rate;
                    newAvgCost = newAmount > 0 ? currentTotalCost / newAmount : 0;
                    NewAmountPreviewText.Text = $"{newAmount:N2}";
                    NewAvgCostPreviewText.Text = $"₺{newAvgCost:N2}";
                    PreviewDetailText.Text = $"1'e {rate:N0} bölünme: {currentAmount:N2} × {rate:N0} = {newAmount:N2} adet. Toplam maliyet değişmez.";
                    break;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_asset == null) return;

            if (!decimal.TryParse(RateTextBox.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal rate) || rate <= 0)
            {
                MessageBox.Show("Lütfen geçerli bir oran/çarpan giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime actionDate = ActionDatePicker.SelectedDate ?? DateTime.Now;
            int actionType = ActionTypeComboBox.SelectedIndex;

            try
            {
                decimal currentAmount = _asset.TotalAmount;
                decimal currentAvgCost = _asset.AverageCost;
                decimal currentTotalCost = currentAmount * currentAvgCost;

                InvestmentTransactionType txType;
                decimal addedShares;
                decimal unitPrice = 0;
                decimal totalCost = 0;
                string notes;

                switch (actionType)
                {
                    case 0: // Bedelsiz
                        txType = InvestmentTransactionType.BonusShare;
                        addedShares = currentAmount * (rate / 100m);
                        _asset.TotalAmount = currentAmount + addedShares;
                        _asset.AverageCost = _asset.TotalAmount > 0 ? currentTotalCost / _asset.TotalAmount : 0;
                        unitPrice = 0;
                        totalCost = 0;
                        notes = $"%{rate:N0} bedelsiz sermaye artırımı: +{addedShares:N2} adet";
                        break;

                    case 1: // Bedelli
                        txType = InvestmentTransactionType.RightsIssue;
                        addedShares = currentAmount * (rate / 100m);
                        decimal rightsPrice = 0;
                        decimal.TryParse(RightsPriceTextBox?.Text?.Replace(",", ".") ?? "0",
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out rightsPrice);

                        decimal rightsCost = addedShares * rightsPrice;
                        _asset.TotalAmount = currentAmount + addedShares;
                        _asset.AverageCost = _asset.TotalAmount > 0 ? (currentTotalCost + rightsCost) / _asset.TotalAmount : 0;
                        unitPrice = rightsPrice;
                        totalCost = rightsCost;
                        notes = $"%{rate:N0} bedelli sermaye artırımı: +{addedShares:N2} adet @ ₺{rightsPrice:N2}";
                        break;

                    case 2: // Bölünme
                    default:
                        txType = InvestmentTransactionType.Split;
                        addedShares = currentAmount * rate - currentAmount; // net fark
                        _asset.TotalAmount = currentAmount * rate;
                        _asset.AverageCost = _asset.TotalAmount > 0 ? currentTotalCost / _asset.TotalAmount : 0;
                        unitPrice = 0;
                        totalCost = 0;
                        notes = $"1'e {rate:N0} hisse bölünmesi: {currentAmount:N2} → {_asset.TotalAmount:N2}";
                        break;
                }

                // Transaction kaydı
                var transaction = new InvestmentTransaction
                {
                    InvestmentAssetId = _assetId,
                    Type = txType,
                    Amount = addedShares,
                    UnitPrice = unitPrice,
                    Fee = 0,
                    TotalCost = totalCost,
                    Date = actionDate,
                    Notes = notes
                };

                _context.InvestmentTransactions.Add(transaction);
                _context.InvestmentAssets.Update(_asset);
                await _context.SaveChangesAsync();

                MessageBox.Show($"Sermaye işlemi başarıyla kaydedildi.\n\n{notes}", "Başarılı",
                    MessageBoxButton.OK, MessageBoxImage.Information);

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
