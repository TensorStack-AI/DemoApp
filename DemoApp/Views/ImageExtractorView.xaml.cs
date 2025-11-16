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
using TensorStack.Extractors.Common;
using TensorStack.Image;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for ImageExtractorView.xaml
    /// </summary>
    public partial class ImageExtractorView : ViewBase
    {
        private PipelineModel _currentPipeline;
        private ImageInput _sourceImage;
        private ImageInput _resultImage;
        private ImageInput _compareImage;
        private TileMode _tileMode;
        private int _tileSize = 512;
        private int _tileOverlap = 16;
        private bool _invertOutput;
        private bool _mergeOutput;
        private BackgroundMode _selectedBackgroundMode = BackgroundMode.RemoveBackground;
        private int _detections = 0;
        private float _bodyConfidence = 0.4f;
        private float _jointConfidence = 0.1f;
        private float _colorAlpha = 0.8f;
        private float _jointRadius = 7f;
        private float _boneRadius = 8f;
        private float _boneThickness = 1f;
        private bool _isTransparent = false;

        public ImageExtractorView(Settings settings, NavigationService navigationService, IHistoryService historyService, IExtractorService extractorService)
            : base(settings, navigationService, historyService)
        {
            ExtractorService = extractorService;

            ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
            CancelCommand = new AsyncRelayCommand(CancelAsync, CanCancel);
            InitializeComponent();
        }

        public override int Id => (int)View.ImageExtractor;
        public IExtractorService ExtractorService { get; }
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

        public TileMode TileMode
        {
            get { return _tileMode; }
            set { SetProperty(ref _tileMode, value); }
        }

        public int TileSize
        {
            get { return _tileSize; }
            set { SetProperty(ref _tileSize, value); }
        }

        public int TileOverlap
        {
            get { return _tileOverlap; }
            set { SetProperty(ref _tileOverlap, value); }
        }

        public bool InvertOutput
        {
            get { return _invertOutput; }
            set { SetProperty(ref _invertOutput, value); }
        }

        public bool MergeOutput
        {
            get { return _mergeOutput; }
            set { SetProperty(ref _mergeOutput, value); }
        }

        public BackgroundMode SelectedBackgroundMode
        {
            get { return _selectedBackgroundMode; }
            set { SetProperty(ref _selectedBackgroundMode, value); }
        }

        public int Detections
        {
            get { return _detections; }
            set { SetProperty(ref _detections, value); }
        }

        public float BodyConfidence
        {
            get { return _bodyConfidence; }
            set { SetProperty(ref _bodyConfidence, value); }
        }

        public float JointConfidence
        {
            get { return _jointConfidence; }
            set { SetProperty(ref _jointConfidence, value); }
        }

        public float ColorAlpha
        {
            get { return _colorAlpha; }
            set { SetProperty(ref _colorAlpha, value); }
        }

        public float JointRadius
        {
            get { return _jointRadius; }
            set { SetProperty(ref _jointRadius, value); }
        }

        public float BoneRadius
        {
            get { return _boneRadius; }
            set { SetProperty(ref _boneRadius, value); }
        }

        public float BoneThickness
        {
            get { return _boneThickness; }
            set { SetProperty(ref _boneThickness, value); }
        }

        public bool IsTransparent
        {
            get { return _isTransparent; }
            set { SetProperty(ref _isTransparent, value); }
        }


        public override Task OpenAsync(OpenViewArgs args = null)
        {
            CurrentPipeline = ExtractorService.Pipeline;
            return base.OpenAsync(args);
        }


        private async Task ExecuteAsync()
        {
            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();
            CompareImage = default;
            try
            {
                // Run Extractor
                var imageTensor = await ExtractorService.ExecuteAsync(new ExtractorImageRequest
                {
                    Image = _sourceImage,
                    TileMode = _tileMode,
                    MaxTileSize = _tileSize,
                    TileOverlap = _tileOverlap,
                    IsInverted = _invertOutput,
                    MergeInput = _mergeOutput,

                    Mode = _selectedBackgroundMode,

                    Detections = _detections,
                    BodyConfidence = _bodyConfidence,
                    BoneRadius = _boneRadius,
                    BoneThickness = _boneThickness,
                    ColorAlpha = _colorAlpha,
                    IsTransparent = _isTransparent,
                    JointConfidence = _jointConfidence,
                    JointRadius = _jointRadius
                });

                var resultImage = new ImageInput(imageTensor);

                // Save History
                await HistoryService.AddAsync(resultImage, new ExtractorItem
                {
                    Source = View.ImageExtractor,
                    MediaType = MediaType.Image,
                    Model = CurrentPipeline.ExtractorModel.Name,
                    Width = resultImage.Width,
                    Height = resultImage.Height,
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
            return _sourceImage is not null && ExtractorService.IsLoaded && !ExtractorService.IsExecuting;
        }


        private async Task CancelAsync()
        {
            await ExtractorService.CancelAsync();
        }


        private bool CanCancel()
        {
            return ExtractorService.CanCancel;
        }


        private async Task LoadPipelineAsync()
        {
            if (_currentPipeline?.ExtractorModel == null)
            {
                await ExtractorService.UnloadAsync();
                return;
            }

            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();

            await ExtractorService.LoadAsync(_currentPipeline);
            Settings.SetDefault(_currentPipeline.ExtractorModel);

            Progress.Clear();
            Debug.WriteLine($"[{GetType().Name}] [LoadAsync] - {Stopwatch.GetElapsedTime(timestamp)}");
        }


        protected async void SelectedPipelineChanged(object sender, PipelineModel pipeline)
        {
            await LoadPipelineAsync();
        }
    }
}