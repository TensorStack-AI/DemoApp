using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : ViewControl
    {
        public SettingsView(NavigationService navigationService)
            : base(navigationService)
        {
            InitializeComponent();
        }

        public override int Id => (int)View.Settings;
    }
}
