using Avalonia.Controls;
using FinTrack.Avalonia.ViewModels;
using System;

namespace FinTrack.Avalonia.Views;

public partial class ManageCardsWindow : Window
{
    public ManageCardsWindow()
    {
        InitializeComponent();
        
        var vm = new ManageCardsViewModel();
        vm.CloseAction = () => Close();
        DataContext = vm;
    }
}
