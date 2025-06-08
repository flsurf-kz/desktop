using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlsurfDesktop.ViewModels;

namespace FlsurfDesktop.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            if (DataContext is LoginWindowViewModel vm)
                vm.CloseWindow = () => this.Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
