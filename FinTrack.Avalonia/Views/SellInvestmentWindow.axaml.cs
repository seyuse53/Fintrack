using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Data;

namespace FinTrack.Avalonia.Views;

public partial class SellInvestmentWindow : Window
{
    public SellInvestmentWindow()
    {
        InitializeComponent();
    }

    public SellInvestmentWindow(AppDbContext context, int assetId) : this()
    {
        var vm = new SellInvestmentViewModel(context, this, assetId);
        DataContext = vm;
        
        Opened += async (s, e) => await vm.InitializeAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
