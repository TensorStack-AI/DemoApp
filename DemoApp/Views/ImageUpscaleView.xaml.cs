using DemoApp.Common;
using DemoApp.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.Image;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for ImageUpscaleView.xaml
    /// </summary>
    public partial class ImageUpscaleView : ViewBase
    {
        private PipelineModel _currentPipeline;
        private ImageInput _sourceImage;
        private ImageInput _resultImage;
        private ImageInput _compareImage;
        private UpscaleOptions _options;

        public ImageUpscaleView(Settings settings, NavigationService navigationService, IHistoryService historyService, IUpscaleService upscaleService)
            : base(settings, navigationService, historyService)
        {
            Options = new UpscaleOptions();
            UpscaleService = upscaleService;
            ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
            CancelCommand = new AsyncRelayCommand(CancelAsync, CanCancel);
            InitializeComponent();
        }

        public override int Id => (int)View.ImageUpscale;
        public IUpscaleService UpscaleService { get; }
        public AsyncRelayCommand ExecuteCommand { get; set; }
        public AsyncRelayCommand CancelCommand { get; set; }

        public PipelineModel CurrentPipeline
        {
            get { return _currentPipeline; }
            set { SetProperty(ref _currentPipeline, value); }
        }

        public ImageInput SourceImage
        {
            get { return _sourceImage; }
            set { SetProperty(ref _sourceImage, value); }
        }

        public ImageInput ResultImage
        {
            get { return _resultImage; }
            set { SetProperty(ref _resultImage, value); }
        }

        public ImageInput CompareImage
        {
            get { return _compareImage; }
            set { SetProperty(ref _compareImage, value); }
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
            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();
            CompareImage = default;
            try
            {
                // Run Upscaler
                var imageTensor = await UpscaleService.ExecuteAsync(new UpscaleImageRequest
                {
                    Image = _sourceImage,
                    TileMode = _options.TileMode,
                    MaxTileSize = _options.TileSize,
                    TileOverlap = _options.TileOverlap
                });

                var resultImage = new ImageInput(imageTensor);

                // Save History
                await HistoryService.AddAsync(resultImage, new UpscaleItem
                {
                    Source = View.ImageUpscale,
                    MediaType = MediaType.Image,
                    Model = _currentPipeline.UpscaleModel.Name,
                    Width = resultImage.Width,
                    Height = resultImage.Height,
                    OriginalWidth = _sourceImage.Width,
                    OriginalHeight = _sourceImage.Height,
                    Options = _options,
                    ScaleFactor = _currentPipeline.UpscaleModel.ScaleFactor,
                    Timestamp = DateTime.UtcNow
                });

                // Set Result
                ResultImage = resultImage;
                CompareImage = SourceImage;

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
            return _sourceImage is not null && UpscaleService.IsLoaded && !UpscaleService.IsExecuting;
        }


        private async Task CancelAsync()
        {
            await UpscaleService.CancelAsync();
        }


        private bool CanCancel()
        {
            return UpscaleService.CanCancel;
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