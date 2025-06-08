using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FlsurfDesktop.Views
{
    public partial class ClientDashboardView : UserControl
    {
        public ClientDashboardView()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools(BackgroundProperty);
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
