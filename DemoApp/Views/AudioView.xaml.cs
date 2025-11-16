using TensorStack.WPF.Services;
using DemoApp.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for AudioView.xaml
    /// </summary>
    public partial class AudioView : ViewBase
    {


        public AudioView(Settings settings, NavigationService navigationService, IHistoryService historyService)
            : base( settings, navigationService, historyService)
        {

            InitializeComponent();
        }

        public override int Id => (int)View.AudioDefault;
    }
}