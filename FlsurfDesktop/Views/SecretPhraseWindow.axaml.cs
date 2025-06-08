using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlsurfDesktop.ViewModels;

namespace FlsurfDesktop.Views
{
    public partial class SecretPhraseWindow : Window
    {
        public SecretPhraseWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            if (DataContext is SecretPhraseWindowViewModel vm)
                vm.CloseWindow = () => this.Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
