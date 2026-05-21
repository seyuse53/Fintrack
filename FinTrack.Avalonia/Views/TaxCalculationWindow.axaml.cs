using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FinTrack.Avalonia.ViewModels;
using FinTrack.Data;

namespace FinTrack.Avalonia.Views
{
    public partial class TaxCalculationWindow : Window
    {
        public TaxCalculationWindow()
        {
            InitializeComponent();
        }

        public TaxCalculationWindow(AppDbContext context) : this()
        {
            DataContext = new TaxCalculationViewModel(context);

            var closeBtn = this.FindControl<Button>("CloseButton");
            if (closeBtn != null)
            {
                closeBtn.Click += (s, e) => Close();
            }
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            
            if (DataContext is TaxCalculationViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}
