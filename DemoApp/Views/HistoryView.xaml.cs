using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;
using DemoApp.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for HistoryView.xaml
    /// </summary>
    public partial class HistoryView : ViewControl
    {
        public HistoryView(NavigationService navigationService, IHistoryService historyService)
            : base( navigationService)
        {
            HistoryService = historyService;

            DataContext = this;
            InitializeComponent();
        }

        public override int Id => (int)View.History;

        public IHistoryService HistoryService { get; }
    }

}
