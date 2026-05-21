using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Core.Models;
using FinTrack.Data;

namespace FinTrack.Avalonia.Views;

public partial class EditInvestmentAssetWindow : Window
{
    public EditInvestmentAssetWindow()
    {
        InitializeComponent();
    }

    public EditInvestmentAssetWindow(AppDbContext context, InvestmentAsset asset) : this()
    {
        var vm = new EditInvestmentAssetViewModel(context, this, asset);
        DataContext = vm;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
