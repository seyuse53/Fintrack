using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Data;

namespace FinTrack.Avalonia.Views
{
    public partial class BudgetView : UserControl
    {
        public BudgetView()
        {
            InitializeComponent();
        }

        public BudgetView(AppDbContext context) : this()
        {
            DataContext = new BudgetViewModel(context);
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            
            if (DataContext is BudgetViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}
