using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Data;

namespace FinTrack.Avalonia.Views;

public partial class InvestmentDetailWindow : Window
{
    public InvestmentDetailWindow()
    {
        InitializeComponent();
    }

    public InvestmentDetailWindow(AppDbContext context, int assetId) : this()
    {
        var vm = new InvestmentDetailViewModel(context, this, assetId);
        DataContext = vm;
        
        Opened += async (s, e) => await vm.InitializeAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
