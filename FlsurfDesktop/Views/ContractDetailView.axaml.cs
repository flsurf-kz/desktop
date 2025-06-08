using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlsurfDesktop.ViewModels;

namespace FlsurfDesktop.Views
{
    public partial class ContractDetailView : Window
    {
        public ContractDetailView()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            if (DataContext is ContractDetailViewModel vm)
                vm.CloseWindow = () => this.Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
