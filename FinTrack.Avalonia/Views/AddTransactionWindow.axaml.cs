using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Data;

namespace FinTrack.Avalonia.Views;

public partial class AddTransactionWindow : Window
{
    public AddTransactionWindow()
    {
        InitializeComponent();
    }

    public AddTransactionWindow(AppDbContext context) : this()
    {
        var vm = new AddTransactionViewModel(context, this);
        DataContext = vm;
        
        // Initialize async data
        Opened += async (s, e) => await vm.InitializeAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
