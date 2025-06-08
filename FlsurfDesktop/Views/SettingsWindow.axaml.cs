using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlsurfDesktop.ViewModels;

namespace FlsurfDesktop.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            if (DataContext is SettingsWindowViewModel vm)
                vm.CloseWindow = () => this.Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
