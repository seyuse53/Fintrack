using Avalonia.Controls;
using FinTrack.Avalonia.ViewModels;

namespace FinTrack.Avalonia.Views;

public partial class ManageAccountsWindow : Window
{
    public ManageAccountsWindow()
    {
        InitializeComponent();
        
        var vm = new ManageAccountsViewModel();
        vm.CloseAction = () => Close(true);
        DataContext = vm;
    }
}
