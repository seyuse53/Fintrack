using Avalonia.Controls;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Data;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Avalonia.Views;

public partial class EditTransactionWindow : Window
{
    public EditTransactionWindow()
    {
        InitializeComponent();
    }

    public EditTransactionWindow(int transactionId) : this()
    {
        var context = AppDbContext.CreateNew();
        if (context != null)
        {
            var vm = new EditTransactionViewModel(context, this, transactionId);
            DataContext = vm;
            _ = vm.InitializeAsync();
        }
    }
}
