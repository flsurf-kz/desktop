using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FlsurfDesktop.Views
{
    public partial class FreelancerDashboardView : UserControl
    {
        public FreelancerDashboardView()
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
