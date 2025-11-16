using DemoApp.Common;
using DemoApp.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Pipeline;
using TensorStack.Video;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for VideoUpscaleView.xaml
    /// </summary>
    public partial class VideoUpscaleView : ViewBase
    {
        private PipelineModel _currentPipeline;
        private VideoInputStream _sourceVideo;
        private VideoInputStream _resultVideo;
        private VideoInputStream _compareVideo;
        private UpscaleOptions _options;
        private IProgress<RunProgress> _progressCallback;

        public VideoUpscaleView(Settings settings, NavigationService navigationService, IHistoryService historyService, IUpscaleService upscaleService)
            : base(settings, navigationService, historyService)
        {
            Options = new UpscaleOptions();
            UpscaleService = upscaleService;
            ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
            CancelCommand = new AsyncRelayCommand(CancelAsync, CanCancel);
            _progressCallback = new Progress<RunProgress>(OnProgress);
            InitializeComponent();
        }

        public override int Id => (int)View.VideoUpscale;
        public IUpscaleService UpscaleService { get; }
        public AsyncRelayCommand ExecuteCommand { get; set; }
        public AsyncRelayCommand CancelCommand { get; set; }

        public PipelineModel CurrentPipeline
        {
            get { return _currentPipeline; }
            set { SetProperty(ref _currentPipeline, value); }
        }

        public VideoInputStream SourceVideo
        {
            get { return _sourceVideo; }
            set { SetProperty(ref _sourceVideo, value); }
        }

        public VideoInputStream ResultVideo
        {
            get { return _resultVideo; }
            set { SetProperty(ref _resultVideo, value); }
        }

        public VideoInputStream CompareVideo
        {
            get { return _compareVideo; }
            set { SetProperty(ref _compareVideo, value); }
        }

        public UpscaleOptions Options
        {
            get { return _options; }
            set { SetProperty(ref _options, value); }
        }


        public override Task OpenAsync(OpenViewArgs args = null)
        {
            CurrentPipeline = UpscaleService.Pipeline;
            return base.OpenAsync(args);
        }


        private async Task ExecuteAsync()
        {
            await ResultControl.ClearAsync();
            var timestamp = Stopwatch.GetTimestamp();

            try
            {
                // Run Upscaler
                var upscaledVideo = await UpscaleService.ExecuteAsync(new UpscaleVideoRequest
                {
                    VideoStream = _sourceVideo,
                    TileMode = _options.TileMode,
                    MaxTileSize = _options.TileSize,
                    TileOverlap = _options.TileOverlap,
                }, _progressCallback);

                // Save History
                var resultVideo = await HistoryService.AddAsync(upscaledVideo, new UpscaleItem
                {
                    Source = View.VideoUpscale,
                    MediaType = MediaType.Video,
                    Model = _currentPipeline.UpscaleModel.Name,
                    Width = _sourceVideo.Width * _currentPipeline.UpscaleModel.ScaleFactor,
                    Height = _sourceVideo.Height * _currentPipeline.UpscaleModel.ScaleFactor,
                    OriginalWidth = _sourceVideo.Width,
                    OriginalHeight = _sourceVideo.Height,
                    Options = _options,
                    ScaleFactor = _currentPipeline.UpscaleModel.ScaleFactor,
                    Timestamp = DateTime.UtcNow
                });

                // Set Result
                ResultVideo = resultVideo;
                CompareVideo = SourceVideo;

                Debug.WriteLine($"[{GetType().Name}] [ExecuteAsync] - Complete: {Stopwatch.GetElapsedTime(timestamp)}");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[{GetType().Name}] [ExecuteAsync] - Cancelled: {Stopwatch.GetElapsedTime(timestamp)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{GetType().Name}] [ExecuteAsync] - Error: {Stopwatch.GetElapsedTime(timestamp)}");
                await DialogService.ShowErrorAsync("ExecuteAsync", ex.Message);
            }

            Progress.Clear();
        }


        private bool CanExecute()
        {
            return _sourceVideo is not null && UpscaleService.IsLoaded && !UpscaleService.IsExecuting;
        }


        private async Task CancelAsync()
        {
            await UpscaleService.CancelAsync();
        }


        private bool CanCancel()
        {
            return UpscaleService.CanCancel;
        }


        private void OnProgress(RunProgress progress)
        {
            Progress.Update(progress.Value + 1, progress.Maximum, progress.Message);
        }


        private async Task LoadPipelineAsync()
        {
            if (_currentPipeline?.UpscaleModel == null)
            {
                await UpscaleService.UnloadAsync();
                return;
            }

            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();

            await UpscaleService.LoadAsync(_currentPipeline);
            Settings.SetDefault(_currentPipeline.UpscaleModel);

            Progress.Clear();
            Debug.WriteLine($"[{GetType().Name}] [LoadAsync] - {Stopwatch.GetElapsedTime(timestamp)}");
        }


        protected async void SelectedPipelineChanged(object sender, PipelineModel pipeline)
        {
            await LoadPipelineAsync();
        }

    }
}