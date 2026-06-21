using Avalonia.Controls;
using FinTrack.Avalonia.ViewModels;
using System;

namespace FinTrack.Avalonia.Views;

public partial class UpdateBesValuesWindow : Window
{
    public UpdateBesValuesWindow()
    {
        InitializeComponent();
    }

    public UpdateBesValuesWindow(UpdateBesValuesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (sender, args) => Close();
    }
}
