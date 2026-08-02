using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FinTrack.Core.Models;
using FinTrack.Data;
using FinTrack.Avalonia.Localization;

namespace FinTrack.Avalonia.ViewModels;

public partial class EditInvestmentAssetViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly global::Avalonia.Controls.Window _ownerWindow;
    private readonly InvestmentAsset _asset;

    [ObservableProperty]
    private string _symbol = "";

    [ObservableProperty]
    private string _name = "";

    public ObservableCollection<string> Categories { get; } = new() 
    { 
        LocalizationService.GetString("Category_Gold"), 
        LocalizationService.GetString("Category_ForeignCurrency"), 
        LocalizationService.GetString("Category_Stock"), 
        LocalizationService.GetString("Category_Crypto"), 
        LocalizationService.GetString("Category_Fund"), 
        LocalizationService.GetString("Category_BES"), 
        LocalizationService.GetString("Global_Other") 
    };

    [ObservableProperty]
    private string _selectedCategory = LocalizationService.GetString("Global_Other");

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _hasError;

    public EditInvestmentAssetViewModel(AppDbContext context, global::Avalonia.Controls.Window ownerWindow, InvestmentAsset asset)
    {
        _context = context;
        _ownerWindow = ownerWindow;
        _asset = asset;

        Symbol = _asset.Symbol;
        Name = _asset.Name;
        if (Categories.Contains(_asset.Category ?? ""))
        {
            SelectedCategory = _asset.Category!;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        HasError = false;
        if (string.IsNullOrWhiteSpace(Symbol) || string.IsNullOrWhiteSpace(Name))
        {
            ShowError(LocalizationService.GetString("AddInvestment_ErrEmpty"));
            return;
        }

        try
        {
            _asset.Symbol = Symbol.ToUpper();
            _asset.Name = Name;
            _asset.Category = SelectedCategory;

            _context.InvestmentAssets.Update(_asset);
            await _context.SaveChangesAsync();

            _ownerWindow.Close(true);
        }
        catch (System.Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _ownerWindow.Close(false);
    }

    private void ShowError(string msg)
    {
        HasError = true;
        ErrorMessage = msg;
    }
}
