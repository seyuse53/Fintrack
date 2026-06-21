using Avalonia;
using Avalonia.Controls;
using System;
using System.ComponentModel;
using FinTrack.Avalonia.ViewModels;

namespace FinTrack.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += MainWindow_DataContextChanged;
    }

    private void MainWindow_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += Vm_PropertyChanged;
            UpdateWindowSize(vm.CurrentView);
        }
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentView))
        {
            if (DataContext is MainWindowViewModel vm)
            {
                UpdateWindowSize(vm.CurrentView);
            }
        }
    }

    private void UpdateWindowSize(ViewModelBase? currentView)
    {
        if (currentView is LoginViewModel)
        {
            // Giriş ekranı için pencereyi küçük, şık ve sabit yapalım
            CanResize = false;
            Width = 420;
            Height = 460;
            Title = "KT FinTrack Erişim";
            CenterWindow();
        }
        else
        {
            // Ana uygulama ekranı için pencereyi büyük ve yeniden boyutlandırılabilir yapalım
            CanResize = true;
            Width = 1200;
            Height = 700;
            Title = "KT FinTrack - Kişisel Finans Takip";
            CenterWindow();
        }
    }

    private void CenterWindow()
    {
        var screen = Screens.Primary ?? Screens.ScreenFromVisual(this);
        if (screen != null)
        {
            var scaling = screen.Scaling;
            var x = screen.WorkingArea.X + (int)((screen.WorkingArea.Width - Width * scaling) / 2);
            var y = screen.WorkingArea.Y + (int)((screen.WorkingArea.Height - Height * scaling) / 2);
            Position = new PixelPoint(x, y);
        }
    }
}