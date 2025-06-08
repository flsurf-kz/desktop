using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlsurfDesktop.ViewModels;

namespace FlsurfDesktop.Views
{
    public partial class SessionDetailView : Window
    {
        public SessionDetailView()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            if (DataContext is SessionDetailViewModel vm)
                vm.CloseWindow = () => this.Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
