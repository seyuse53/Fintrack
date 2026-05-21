using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Data;

namespace FinTrack.Avalonia.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        public ReportsView(AppDbContext context) : this()
        {
            DataContext = new ReportsViewModel(context);
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            
            if (DataContext is ReportsViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}
