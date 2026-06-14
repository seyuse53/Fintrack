using Avalonia.Controls;
using FinTrack.Avalonia.ViewModels;

namespace FinTrack.Avalonia.Views;

public partial class AccountsView : UserControl
{
    public AccountsView()
    {
        InitializeComponent();
        this.Loaded += AccountsView_Loaded;
        this.Unloaded += AccountsView_Unloaded;
    }

    private void AccountsView_Loaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is AccountsViewModel vm)
        {
            vm.RequestManageAccounts -= Vm_RequestManageAccounts;
            vm.RequestTransfer -= Vm_RequestTransfer;
            vm.RequestAccountDetails -= Vm_RequestAccountDetails;

            vm.RequestManageAccounts += Vm_RequestManageAccounts;
            vm.RequestTransfer += Vm_RequestTransfer;
            vm.RequestAccountDetails += Vm_RequestAccountDetails;
        }
    }

    private void AccountsView_Unloaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is AccountsViewModel vm)
        {
            vm.RequestManageAccounts -= Vm_RequestManageAccounts;
            vm.RequestTransfer -= Vm_RequestTransfer;
            vm.RequestAccountDetails -= Vm_RequestAccountDetails;
        }
    }

    private global::Avalonia.Controls.Window? GetParentWindow()
    {
        return (this.VisualRoot as global::Avalonia.Controls.Window) 
            ?? (global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }

    private async void Vm_RequestManageAccounts()
    {
        var window = new ManageAccountsWindow();
        var parent = GetParentWindow();
        if (parent != null)
        {
            await window.ShowDialog(parent);
            if (DataContext is AccountsViewModel vm)
            {
                await vm.LoadDataAsync();
            }
        }
    }

    private async void Vm_RequestTransfer(int? accountId)
    {
        var window = new TransferWindow(accountId);
        var parent = GetParentWindow();
        if (parent != null)
        {
            await window.ShowDialog(parent);
            if (DataContext is AccountsViewModel vm)
            {
                await vm.LoadDataAsync();
            }
        }
    }

    private async void Vm_RequestAccountDetails(int? accountId)
    {
        var window = new AccountDetailWindow(accountId);
        var parent = GetParentWindow();
        if (parent != null)
        {
            await window.ShowDialog(parent);
            if (DataContext is AccountsViewModel vm)
            {
                await vm.LoadDataAsync();
            }
        }
    }
}
