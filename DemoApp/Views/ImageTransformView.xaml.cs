using DemoApp.Common;
using DemoApp.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Common;
using TensorStack.Common.Tensor;
using TensorStack.Image;
using TensorStack.StableDiffusion.Common;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for ImageTransformView.xaml
    /// </summary>
    public partial class ImageTransformView : ViewBase
    {
        private PipelineModel _currentPipeline;
        private ImageInput _sourceImage;
        private ImageInput _resultImage;
        private ImageInput _compareImage;
        private ImageInput _extractorImage;
        private ImageGenerateOptions _options;

        public ImageTransformView(Settings settings, NavigationService navigationService, IHistoryService historyService, IDiffusionService diffusionService, IUpscaleService upscaleService, IExtractorService extractorService)
            : base(settings, navigationService, historyService)
        {
            DiffusionService = diffusionService;
            UpscaleService = upscaleService;
            ExtractorService = extractorService;
            ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
            CancelCommand = new AsyncRelayCommand(CancelAsync, CanCancel);
            InitializeComponent();
        }

        public override int Id => (int)View.ImageTransform;
        public IDiffusionService DiffusionService { get; }
        public IUpscaleService UpscaleService { get; }
        public IExtractorService ExtractorService { get; }
        public AsyncRelayCommand LoadCommand { get; set; }
        public AsyncRelayCommand UnloadCommand { get; set; }
        public AsyncRelayCommand ExecuteCommand { get; set; }
        public AsyncRelayCommand CancelCommand { get; set; }

        public PipelineModel CurrentPipeline
        {
            get { return _currentPipeline; }
            set { SetProperty(ref _currentPipeline, value); ExtractorImage = default; }
        }

        public ImageInput SourceImage
        {
            get { return _sourceImage; }
            set { SetProperty(ref _sourceImage, value); _extractorImage = default; }
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

        public ImageInput ExtractorImage
        {
            get { return _extractorImage; }
            set { SetProperty(ref _extractorImage, value); }
        }


        public ImageGenerateOptions Options
        {
            get { return _options; }
            set { SetProperty(ref _options, value); }
        }


        public override Task OpenAsync(OpenViewArgs args = null)
        {
            CurrentPipeline = DiffusionService.Pipeline;
            if (DiffusionService.IsLoaded)
            {
                SetDefaultOptions(DiffusionService.DefaultOptions);
            }
            return base.OpenAsync(args);
        }


        private async Task ExecuteAsync()
        {
            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();
            CompareImage = default;

            try
            {

                ImageInput inputImage = _sourceImage;
                ImageInput controlImage = default;

                // Run Extractor
                if (ExtractorService.IsLoaded && ExtractorImage == null)
                {
                    ExtractorImage = new ImageInput(await ExtractorService.ExecuteAsync(new ExtractorImageRequest
                    {
                        Image = _sourceImage
                    }));
                }


                if (_currentPipeline.ControlModel is not null)
                {
                    inputImage = _options.Strength >= 1 ? default : _sourceImage;
                    controlImage = _extractorImage ?? _sourceImage;
                }


                // Run Diffusion
                var resultTensor = await DiffusionService.ExecuteAsync(_options with
                {
                    InputImage = inputImage,
                    InputControlImage = controlImage
                });


                // Run Upscaler
                if (UpscaleService.IsLoaded)
                {
                    resultTensor = await UpscaleService.ExecuteAsync(new UpscaleImageRequest
                    {
                        Image = resultTensor
                    });
                }

                var resultImage = new ImageInput(resultTensor);

                // Save History
                await HistoryService.AddAsync(resultImage, new DiffusionItem
                {
                    Source = View.ImageGenerate,
                    MediaType = MediaType.Image,
                    Model = _currentPipeline.DiffusionModel.Name,
                    Control = _currentPipeline.ControlModel?.Name,
                    Extractor = _currentPipeline.ExtractorModel?.Name,
                    Upscaler = _currentPipeline.UpscaleModel?.Name,
                    Width = resultImage.Width,
                    Height = resultImage.Height,
                    Timestamp = DateTime.UtcNow
                });

                // Set Result
                ResultImage = resultImage;
                CompareImage = controlImage ?? SourceImage;

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
            return DiffusionService.IsLoaded && !DiffusionService.IsExecuting && SourceImage is not null;
        }


        private async Task CancelAsync()
        {
            await DiffusionService.CancelAsync();
        }


        private bool CanCancel()
        {
            return DiffusionService.CanCancel;
        }


        private void SetDefaultOptions(GenerateOptions options)
        {
            Options = new ImageGenerateOptions
            {
                Strength = 0.65f,
                ControlNetStrength = 0.75f,
                Width = options.Width,
                Height = options.Height,
                Steps = options.Steps,
                Scheduler = options.Scheduler,
                GuidanceScale = options.GuidanceScale
            };
        }


        private async Task LoadPipelineAsync()
        {
            if (_currentPipeline.DiffusionModel == null)
                await DiffusionService.UnloadAsync();
            if (_currentPipeline.UpscaleModel == null)
                await UpscaleService.UnloadAsync();
            if (_currentPipeline.ExtractorModel == null)
                await ExtractorService.UnloadAsync();

            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();
            if (_currentPipeline.DiffusionModel is not null)
            {
                await DiffusionService.LoadAsync(_currentPipeline);
                SetDefaultOptions(DiffusionService.DefaultOptions);
                Settings.SetDefault(_currentPipeline.DiffusionModel);
            }

            if (_currentPipeline.ExtractorModel is not null)
            {
                await ExtractorService.LoadAsync(_currentPipeline);
                Settings.SetDefault(_currentPipeline.ExtractorModel);
            }

            if (_currentPipeline.UpscaleModel is not null)
            {
                await UpscaleService.LoadAsync(_currentPipeline);
                Settings.SetDefault(_currentPipeline.UpscaleModel);
            }

            Progress.Clear();
            Debug.WriteLine($"[{GetType().Name}] [LoadAsync] - {Stopwatch.GetElapsedTime(timestamp)}");
        }


        protected async void SelectedPipelineChanged(object sender, PipelineModel pipeline)
        {
            await LoadPipelineAsync();
        }

    }
}