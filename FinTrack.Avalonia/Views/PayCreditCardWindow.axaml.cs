using Avalonia.Controls;
using FinTrack.Avalonia.ViewModels;
using System;

namespace FinTrack.Avalonia.Views;

public partial class PayCreditCardWindow : Window
{
    public PayCreditCardWindow()
    {
        InitializeComponent();
    }

    public PayCreditCardWindow(int cardId) : this()
    {
        var vm = new PayCreditCardViewModel(cardId);
        vm.CloseAction = () => Close();
        DataContext = vm;
    }
}
