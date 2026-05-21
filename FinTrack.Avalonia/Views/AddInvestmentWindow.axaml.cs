using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Data;

namespace FinTrack.Avalonia.Views;

public partial class AddInvestmentWindow : Window
{
    public AddInvestmentWindow()
    {
        InitializeComponent();
    }

    public AddInvestmentWindow(AppDbContext context) : this()
    {
        var vm = new AddInvestmentViewModel(context, this);
        DataContext = vm;
        
        Opened += async (s, e) => await vm.InitializeAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
