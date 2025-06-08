using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FlsurfDesktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            // DataContext задаётся в XAML или через DI в Program.cs
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
