using Avalonia.Controls;
using FinTrack.Avalonia.ViewModels;

namespace FinTrack.Avalonia.Views;

public partial class AccountDetailWindow : Window
{
    public AccountDetailWindow()
    {
        InitializeComponent();
    }

    public AccountDetailWindow(int? accountId) : this()
    {
        var vm = new AccountDetailViewModel(accountId);
        vm.CloseAction = () => Close();
        vm.RequestEditTransaction += async (transactionId) =>
        {
            var editWindow = new EditTransactionWindow(transactionId);
            var result = await editWindow.ShowDialog<bool?>(this);
            if (result == true)
            {
                await vm.RefreshAsync();
            }
        };
        vm.ConfirmDeleteFunc = async (message) =>
        {
            var confirmDialog = new ConfirmDialog(
                "İşlemi Sil",
                message,
                "Evet, Sil",
                "İptal");
            
            confirmDialog.SetHighContrast(true);
            var result = await confirmDialog.ShowDialog<bool?>(this);
            return result == true;
        };
        DataContext = vm;
    }
}
