// Copyright (c) TensorStack. All rights reserved.
// Licensed under the Apache 2.0 License.
using TensorStack.WPF.Controls;
using System.Threading.Tasks;
using DemoApp.Common;
using TensorStack.WPF;

namespace DemoApp.Dialogs
{
    /// <summary>
    /// Interaction logic for PreviewImageDialog.xaml
    /// </summary>
    public partial class PreviewImageDialog : DialogControl
    {
        private HistoryItem _historyItem;

        public PreviewImageDialog()
        {
            OkCommand = new AsyncRelayCommand(Ok);
            InitializeComponent();
        }

        public AsyncRelayCommand OkCommand { get; }

        public HistoryItem HistoryItem
        {
            get { return _historyItem; }
            set { SetProperty(ref _historyItem, value); }
        }

        public Task<bool> ShowDialogAsync(HistoryItem historyItem)
        {
            HistoryItem = historyItem;
            return base.ShowDialogAsync();
        }


        private Task Ok()
        {
            return base.SaveAsync();
        }

    }
}
