using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinTrack.Core.Models;
using FinTrack.Core.Services;
using FinTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Avalonia.ViewModels
{
    public partial class TaxCalculationViewModel : ViewModelBase
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<int> _years = new();

        [ObservableProperty]
        private int _selectedYear;

        [ObservableProperty]
        private string _totalGainText = "₺0,00";

        [ObservableProperty]
        private string _totalGainColorHex = "#2C3E50";

        [ObservableProperty]
        private string _totalTaxText = "₺0,00";

        [ObservableProperty]
        private string _totalDividendText = "₺0,00";

        [ObservableProperty]
        private string _netReturnText = "₺0,00";

        [ObservableProperty]
        private string _netReturnColorHex = "#2C3E50";

        [ObservableProperty]
        private string _transactionCountText = "(0 işlem)";

        [ObservableProperty]
        private ObservableCollection<TaxDetailViewModel> _taxDetails = new();

        public TaxCalculationViewModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            var yearList = await _context.InvestmentTransactions
                .Select(t => t.Date.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            if (!yearList.Contains(DateTime.Now.Year))
                yearList.Insert(0, DateTime.Now.Year);

            yearList = yearList.OrderByDescending(y => y).ToList();

            Years = new ObservableCollection<int>(yearList);

            if (Years.Count > 0)
            {
                SelectedYear = Years.First(); // This will trigger OnSelectedYearChanged automatically
            }
        }

        async partial void OnSelectedYearChanged(int value)
        {
            await CalculateTaxAsync(value);
        }

        private async Task CalculateTaxAsync(int year)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                var transactions = await _context.InvestmentTransactions
                    .Include(t => t.InvestmentAsset)
                    .Where(t => t.Date >= startDate && t.Date <= endDate &&
                                (t.Type == InvestmentTransactionType.Sell ||
                                 t.Type == InvestmentTransactionType.Dividend))
                    .OrderBy(t => t.Date)
                    .ToListAsync();

                var taxItems = new List<TaxDetailViewModel>();
                decimal totalGain = 0;
                decimal totalTax = 0;
                decimal totalDividend = 0;

                foreach (var tx in transactions)
                {
                    var asset = tx.InvestmentAsset;
                    if (asset == null) continue;

                    string category = asset.Category ?? "Diğer";

                    if (tx.Type == InvestmentTransactionType.Sell)
                    {
                        decimal buyCost = tx.Amount * asset.AverageCost;
                        decimal sellRevenue = tx.TotalCost;
                        decimal gain = sellRevenue - buyCost;
                        decimal taxRate = SettingsManager.GetTaxRateForCategory(category);
                        decimal taxAmount = gain > 0 ? gain * (taxRate / 100m) : 0;

                        totalGain += gain;
                        totalTax += taxAmount;

                        taxItems.Add(new TaxDetailViewModel
                        {
                            Date = tx.Date,
                            AssetName = $"{asset.Symbol} ({asset.Name})",
                            TypeText = "🔴 Satış",
                            TypeColorHex = "#E74C3C",
                            Amount = tx.Amount,
                            BuyCost = buyCost,
                            SellRevenue = sellRevenue,
                            Gain = gain,
                            GainText = gain >= 0 ? $"+₺{gain:N2}" : $"-₺{Math.Abs(gain):N2}",
                            GainColorHex = gain >= 0 ? "#27AE60" : "#E74C3C",
                            TaxRate = taxRate,
                            TaxRateText = $"%{taxRate:N0}",
                            TaxAmount = taxAmount
                        });
                    }
                    else if (tx.Type == InvestmentTransactionType.Dividend)
                    {
                        decimal dividendAmount = tx.TotalCost;
                        decimal dividendTaxRate = SettingsManager.GetDividendTaxRate();
                        decimal dividendTax = dividendAmount * (dividendTaxRate / 100m);

                        totalDividend += dividendAmount;
                        totalGain += dividendAmount;
                        totalTax += dividendTax;

                        taxItems.Add(new TaxDetailViewModel
                        {
                            Date = tx.Date,
                            AssetName = $"{asset.Symbol} ({asset.Name})",
                            TypeText = "💰 Temettü",
                            TypeColorHex = "#F39C12",
                            Amount = tx.Amount,
                            BuyCost = 0,
                            SellRevenue = dividendAmount,
                            Gain = dividendAmount,
                            GainText = $"+₺{dividendAmount:N2}",
                            GainColorHex = "#27AE60",
                            TaxRate = dividendTaxRate,
                            TaxRateText = $"%{dividendTaxRate:N0}",
                            TaxAmount = dividendTax
                        });
                    }
                }

                TotalGainText = totalGain >= 0 ? $"+₺{totalGain:N2}" : $"-₺{Math.Abs(totalGain):N2}";
                TotalGainColorHex = totalGain >= 0 ? "#27AE60" : "#E74C3C";

                TotalTaxText = $"₺{totalTax:N2}";
                TotalDividendText = $"₺{totalDividend:N2}";

                decimal netReturn = totalGain - totalTax;
                NetReturnText = netReturn >= 0 ? $"+₺{netReturn:N2}" : $"-₺{Math.Abs(netReturn):N2}";
                NetReturnColorHex = netReturn >= 0 ? "#2980B9" : "#E74C3C";

                TransactionCountText = $"({taxItems.Count} işlem)";
                TaxDetails = new ObservableCollection<TaxDetailViewModel>(taxItems);
            }
            catch
            {
                // Silently handle or log
            }
        }
    }

    public class TaxDetailViewModel
    {
        public DateTime Date { get; set; }
        public string AssetName { get; set; } = "";
        public string TypeText { get; set; } = "";
        public string TypeColorHex { get; set; } = "#333333";
        public decimal Amount { get; set; }
        public decimal BuyCost { get; set; }
        public decimal SellRevenue { get; set; }
        public decimal Gain { get; set; }
        public string GainText { get; set; } = "";
        public string GainColorHex { get; set; } = "#333333";
        public decimal TaxRate { get; set; }
        public string TaxRateText { get; set; } = "";
        public decimal TaxAmount { get; set; }
    }
}
