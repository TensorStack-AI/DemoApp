using DemoApp.Common;
using DemoApp.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Common;
using TensorStack.Image;
using TensorStack.TextGeneration.Common;
using TensorStack.TextGeneration.Pipelines.Florence;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for ImageDetectView.xaml
    /// </summary>
    public partial class ImageDetectView : ViewBase
    {
        private PipelineModel _currentPipeline;
        private ImageInput _sourceImage;
        private int _topK = 3;
        private int _beams = 0;
        private int _seed = 0;
        private float _topP = 1f;
        private float _temperature = 1.5f;
        private float _lengthPenalty = 1f;
        private int _diversityLength = 1024;
        private EarlyStopping _earlyStopping = EarlyStopping.None;
        private string _searchObjectText;
        private string _searchMaskText;
        private string _searchPhraseText;
        private string _captionResult;

        public ImageDetectView(Settings settings, NavigationService navigationService, IHistoryService historyService, IDetectService detectService)
            : base(settings, navigationService, historyService)
        {
            DetectService = detectService;
                    ExecuteCommand = new AsyncRelayCommand<TaskType>(ExecuteAsync, CanExecute);
            CancelCommand = new AsyncRelayCommand(CancelAsync, CanCancel);
            AddHistoryCommand = new AsyncRelayCommand(AddHistoryAsync, CanAddHistory);
            InitializeComponent();
        }

        public override int Id => (int)View.ImageDetect;
        public IDetectService DetectService { get; }
        public AsyncRelayCommand<TaskType> ExecuteCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public AsyncRelayCommand AddHistoryCommand { get; }

        public PipelineModel CurrentPipeline
        {
            get { return _currentPipeline; }
            set { SetProperty(ref _currentPipeline, value); }
        }

        public ImageInput SourceImage
        {
            get { return _sourceImage; }
            set { SetProperty(ref _sourceImage, value); ResetView(); }
        }

        public int TopK
        {
            get { return _topK; }
            set { SetProperty(ref _topK, value); }
        }

        public int Beams
        {
            get { return _beams; }
            set { SetProperty(ref _beams, value); }
        }

        public int Seed
        {
            get { return _seed; }
            set { SetProperty(ref _seed, value); }
        }

        public float TopP
        {
            get { return _topP; }
            set { SetProperty(ref _topP, value); }
        }

        public float Temperature
        {
            get { return _temperature; }
            set { SetProperty(ref _temperature, value); }
        }

        public float LengthPenalty
        {
            get { return _lengthPenalty; }
            set { SetProperty(ref _lengthPenalty, value); }
        }

        public int DiversityLength
        {
            get { return _diversityLength; }
            set { SetProperty(ref _diversityLength, value); }
        }

        public EarlyStopping EarlyStopping
        {
            get { return _earlyStopping; }
            set { SetProperty(ref _earlyStopping, value); }
        }

        public string SearchMaskText
        {
            get { return _searchMaskText; }
            set { SetProperty(ref _searchMaskText, value); }
        }

        public string SearchObjectText
        {
            get { return _searchObjectText; }
            set { SetProperty(ref _searchObjectText, value); }
        }

        public string SearchPhraseText
        {
            get { return _searchPhraseText; }
            set { SetProperty(ref _searchPhraseText, value); }
        }

        public string CaptionResult
        {
            get { return _captionResult; }
            set { SetProperty(ref _captionResult, value); }
        }


        public override Task OpenAsync(OpenViewArgs args = null)
        {
            CurrentPipeline = DetectService.Pipeline;
            return base.OpenAsync(args);
        }


        private async Task ExecuteAsync(TaskType taskType)
        {
            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();
            ResetView();
            try
            {
                // Run Detect
                var promptText = GetPromptText(taskType);
                var detectionResult = await DetectService.ExecuteAsync(new DetectImageRequest
                {
                    Image = _sourceImage,
                    TaskType = taskType,
                    Prompt = promptText,
                    Beams = _beams,
                    TopK = _topK,
                    Seed = _seed,
                    TopP = _topP,
                    Temperature = _temperature,
                    LengthPenalty = 0,
                    MinLength = 3,
                    MaxLength = 1024,
                    NoRepeatNgramSize = 4,
                    DiversityLength = _diversityLength,
                    EarlyStopping = EarlyStopping.None,
                    Region = DetectCanvas.GetSelectedRegion()
                });


                // Set Result
                DetectCanvas.ClearSelection();
                var bestResult = detectionResult.First();

                CaptionResult = bestResult.Result;
                foreach (var result in detectionResult)
                {
                    DetectCanvas.AddOverlay(taskType, result);
                }

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


        private bool CanExecute(TaskType taskType)
        {
            return _sourceImage is not null && DetectService.IsLoaded && !DetectService.IsExecuting;
        }


        private async Task CancelAsync()
        {
            await DetectService.CancelAsync();
        }


        private bool CanCancel()
        {
            return DetectService.CanCancel;
        }


        private async Task AddHistoryAsync()
        {
            // Save History
            var surfaceImage = new ImageInput(DetectCanvas.SaveCanvas());
            await HistoryService.AddAsync(surfaceImage, new DetectItem
            {
                Source = View.ImageDetect,
                MediaType = MediaType.Image,
                Model = CurrentPipeline.DetectModel.Name,
                Timestamp = DateTime.UtcNow
            });
        }


        private bool CanAddHistory()
        {
            return DetectCanvas.Overlays.Count > 0;
        }


        private string GetPromptText(TaskType taskType)
        {
            return taskType switch
            {
                TaskType.REFERRING_EXPRESSION_SEGMENTATION => _searchMaskText,
                TaskType.OPEN_VOCABULARY_DETECTION => _searchObjectText,
                TaskType.CAPTION_TO_PHRASE_GROUNDING => _searchPhraseText,
                _ => string.Empty
            };
        }


        private void ResetView()
        {
            DetectCanvas.ResetView();
        }


        private async Task LoadPipelineAsync()
        {
            if (_currentPipeline?.DetectModel == null)
            {
                await DetectService.UnloadAsync();
                return;
            }

            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();

            await DetectService.LoadAsync(_currentPipeline);
            Settings.SetDefault(_currentPipeline.DetectModel);

            Progress.Clear();
            Debug.WriteLine($"[{GetType().Name}] [LoadAsync] - {Stopwatch.GetElapsedTime(timestamp)}");
        }


        protected async void SelectedPipelineChanged(object sender, PipelineModel pipeline)
        {
            await LoadPipelineAsync();
        }
    }
}