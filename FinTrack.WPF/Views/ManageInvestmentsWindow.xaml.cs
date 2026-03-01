using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.WPF.Views
{
    public partial class ManageInvestmentsWindow : Window
    {
        private readonly AppDbContext _context;

        public ManageInvestmentsWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            Loaded += async (s, e) => await LoadInvestmentsAsync();
        }

        private async Task LoadInvestmentsAsync()
        {
            try
            {
                var assets = await _context.InvestmentAssets
                    .OrderBy(a => a.Symbol)
                    .ToListAsync();

                InvestmentsGrid.ItemsSource = assets;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veriler yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddInvestment_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new AddInvestmentWindow(_context);
            addWin.Owner = this;
            if (addWin.ShowDialog() == true)
            {
                await LoadInvestmentsAsync();
            }
        }

        private async void SellInvestment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int assetId)
            {
                var sellWin = new SellInvestmentWindow(_context, assetId);
                sellWin.Owner = this;
                if (sellWin.ShowDialog() == true)
                {
                    await LoadInvestmentsAsync();
                }
            }
        }

        private async void DeleteInvestment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int assetId)
            {
                var result = MessageBox.Show(
                    "Bu varlığı ve ona bağlı tüm işlem geçmişini silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
                    "Varlık Sil",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var asset = await _context.InvestmentAssets.FindAsync(assetId);
                        if (asset != null)
                        {
                            _context.InvestmentAssets.Remove(asset);
                            await _context.SaveChangesAsync();
                            await LoadInvestmentsAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Silme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
