using DemoApp.Services;
using System;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Views
{
    public abstract class ViewBase : ViewControl
    {
        public ViewBase(Settings settings, NavigationService navigationService, IHistoryService historyService)
            : base(navigationService)
        {
            Settings = settings;
            HistoryService = historyService;
            Progress = new ProgressInfo();
            DownloadCallback = new Progress<double>(OnDownloadProgress);
        }

        public Settings Settings { get; }
        public IHistoryService HistoryService { get; }
        public ProgressInfo Progress { get; }
        public Progress<double> DownloadCallback { get; set; }

        protected virtual void OnDownloadProgress(double value)
        {
            Progress.Update((int)value, 100);
        }
    }
}
