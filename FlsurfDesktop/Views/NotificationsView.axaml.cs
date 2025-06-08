using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FlsurfDesktop.Views
{
    public partial class NotificationsView : UserControl
    {
        public NotificationsView()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
