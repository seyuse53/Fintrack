using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Data;

namespace FinTrack.Avalonia.Views;

public partial class ManageInvestmentsWindow : Window
{
    public ManageInvestmentsWindow()
    {
        InitializeComponent();
    }

    public ManageInvestmentsWindow(AppDbContext context) : this()
    {
        var vm = new ManageInvestmentsViewModel(context, this);
        DataContext = vm;
        
        // Initialize async data
        Opened += async (s, e) => await vm.LoadInvestmentsAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
