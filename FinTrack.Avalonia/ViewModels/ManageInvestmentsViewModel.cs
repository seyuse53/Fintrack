using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FinTrack.Core.Models;
using FinTrack.Data;
using System.Linq;
using FinTrack.Core.Helpers;

namespace FinTrack.Avalonia.ViewModels;

public partial class ManageInvestmentsViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly global::Avalonia.Controls.Window _ownerWindow;

    [ObservableProperty]
    private ObservableCollection<InvestmentAsset> _assets = new();

    public ManageInvestmentsViewModel(AppDbContext context, global::Avalonia.Controls.Window ownerWindow)
    {
        _context = context;
        _ownerWindow = ownerWindow;
    }

    public async Task LoadInvestmentsAsync()
    {
        try
        {
            var data = await _context.InvestmentAssets
                .OrderBy(a => a.Symbol)
                .ToListAsync();

            Assets.Clear();
            foreach (var asset in data)
            {
                Assets.Add(asset);
            }
        }
        catch (System.Exception ex)
        {
            // Error handling placeholder
            AppLogger.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task AddInvestmentAsync()
    {
        var addWin = new FinTrack.Avalonia.Views.AddInvestmentWindow(_context);
        var result = await addWin.ShowDialog<bool>(_ownerWindow);
        if (result)
        {
            await LoadInvestmentsAsync();
        }
    }

    [RelayCommand]
    private async Task SellInvestmentAsync(int assetId)
    {
        var sellWin = new FinTrack.Avalonia.Views.SellInvestmentWindow(_context, assetId);
        var result = await sellWin.ShowDialog<bool>(_ownerWindow);
        if (result)
        {
            await LoadInvestmentsAsync();
        }
    }

    [RelayCommand]
    private async Task EditInvestmentAsync(int assetId)
    {
        var asset = await _context.InvestmentAssets.FindAsync(assetId);
        if (asset == null) return;

        var editWin = new FinTrack.Avalonia.Views.EditInvestmentAssetWindow(_context, asset);
        var result = await editWin.ShowDialog<bool>(_ownerWindow);
        if (result) await LoadInvestmentsAsync();
    }

    [RelayCommand]
    private async Task DeleteInvestmentAsync(int assetId)
    {
        // Simple confirmation via Dialog would be ideal, skipping for now
        var asset = await _context.InvestmentAssets.FindAsync(assetId);
        if (asset != null)
        {
            _context.InvestmentAssets.Remove(asset);
            await _context.SaveChangesAsync();
            await LoadInvestmentsAsync();
        }
    }

    [RelayCommand]
    private async Task ViewHistoryAsync(int assetId)
    {
        var detailWin = new FinTrack.Avalonia.Views.InvestmentDetailWindow(_context, assetId);
        await detailWin.ShowDialog<bool>(_ownerWindow);
    }

    [RelayCommand]
    private void Close()
    {
        _ownerWindow.Close(true);
    }
}
