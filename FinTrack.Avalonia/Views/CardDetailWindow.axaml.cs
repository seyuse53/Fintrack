using Avalonia.Controls;
using FinTrack.Avalonia.ViewModels;
using System;

namespace FinTrack.Avalonia.Views;

public partial class CardDetailWindow : Window
{
    public CardDetailWindow()
    {
        InitializeComponent();
    }

    public CardDetailWindow(int cardId) : this()
    {
        var vm = new CardDetailViewModel(cardId);
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
