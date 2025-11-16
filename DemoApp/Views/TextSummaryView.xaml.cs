using DemoApp.Common;
using DemoApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Common;
using TensorStack.TextGeneration.Common;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for TextSummaryView.xaml
    /// </summary>
    public partial class TextSummaryView : ViewBase
    {
        private PipelineModel _currentPipeline;
        private int _topK = 50;
        private int _beams = 4;
        private int _seed = -1;
        private float _topP = 0.9f;
        private float _temperature = 1f;
        private float _lengthPenalty = 1f;
        private int _diversityLength = 512;
        private int _minLength = 20;
        private int _maxLength = 512;
        private EarlyStopping _earlyStopping = EarlyStopping.None;
        private string _promptText;
        private string _selectedPrefix;
        private bool _isMultipleResult;
        private int _selectedBeam;

        public TextSummaryView(Settings settings, NavigationService navigationService, IHistoryService historyService, ITextService textService)
            : base(settings, navigationService, historyService)
        {
            TextService = textService;
            ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
            CancelCommand = new AsyncRelayCommand(CancelAsync, CanCancel);
            Prefixes = new ObservableCollection<string>();
            SummaryResults = new ObservableCollection<SummaryResult>();
            InitializeComponent();
        }

        public override int Id => (int)View.TextSummary;
        public ITextService TextService { get; }
        public AsyncRelayCommand ExecuteCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public ObservableCollection<string> Prefixes { get; }
        public ObservableCollection<SummaryResult> SummaryResults { get; }
        public SummaryResult SummaryResult => SummaryResults?.FirstOrDefault();

        public PipelineModel CurrentPipeline
        {
            get { return _currentPipeline; }
            set { SetProperty(ref _currentPipeline, value); }
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

        public int MinLength
        {
            get { return _minLength; }
            set { SetProperty(ref _minLength, value); }
        }

        public int MaxLength
        {
            get { return _maxLength; }
            set { SetProperty(ref _maxLength, value); }
        }

        public EarlyStopping EarlyStopping
        {
            get { return _earlyStopping; }
            set { SetProperty(ref _earlyStopping, value); }
        }

        public string PromptText
        {
            get { return _promptText; }
            set { SetProperty(ref _promptText, value); }
        }

        public string SelectedPrefix
        {
            get { return _selectedPrefix; }
            set { SetProperty(ref _selectedPrefix, value); }
        }

        public bool IsMultipleResult
        {
            get { return _isMultipleResult; }
            set { SetProperty(ref _isMultipleResult, value); }
        }

        public int SelectedBeam
        {
            get { return _selectedBeam; }
            set { SetProperty(ref _selectedBeam, value); }
        }


        public override Task OpenAsync(OpenViewArgs args = null)
        {
            CurrentPipeline = TextService.Pipeline;
            return base.OpenAsync(args);
        }


        private async Task ExecuteAsync()
        {
            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate("Generating Results...");
            try
            {
                // Run Summary
                var promptText = string.Concat(_selectedPrefix, _promptText);
                var summaryResults = await TextService.ExecuteAsync(new TextRequest
                {
                    Prompt = promptText,
                    Beams = _beams,
                    TopK = _topK,
                    Seed = _seed,
                    TopP = _topP,
                    Temperature = _temperature,
                    LengthPenalty = _lengthPenalty,
                    MinLength = _minLength,
                    MaxLength = _maxLength,
                    NoRepeatNgramSize = 4,
                    DiversityLength = _diversityLength,
                    EarlyStopping = _earlyStopping,
                });

                SummaryResults.Clear();
                foreach (var summaryResult in summaryResults)
                {
                    SummaryResults.Add(new SummaryResult($"Beam {summaryResult.Beam}", summaryResult.Result, summaryResult.PenaltyScore));
                }
                SelectedBeam = 0;

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
            NotifyPropertyChanged(nameof(SummaryResult));
        }


        private bool CanExecute()
        {
            return !string.IsNullOrWhiteSpace(_promptText) && TextService.IsLoaded && !TextService.IsExecuting;
        }


        private async Task CancelAsync()
        {
            await TextService.CancelAsync();
        }


        private bool CanCancel()
        {
            return TextService.CanCancel;
        }


        private async Task LoadPipelineAsync()
        {
            if (_currentPipeline?.TextModel == null)
            {
                await TextService.UnloadAsync();
                return;
            }

            var timestamp = Stopwatch.GetTimestamp();
            Progress.Indeterminate();

            await TextService.LoadAsync(_currentPipeline);
            Settings.SetDefault(_currentPipeline.TextModel);

            var model = _currentPipeline.TextModel;
            MinLength = model.MinLength;
            MaxLength = model.MaxLength;
            DiversityLength = model.MinLength;
            if (model.Prefixes != null)
            {
                foreach (var prefix in model.Prefixes)
                {
                    Prefixes.Add(prefix);
                }
                SelectedPrefix = Prefixes.FirstOrDefault();
            }

            Progress.Clear();
            Debug.WriteLine($"[{GetType().Name}] [LoadAsync] - {Stopwatch.GetElapsedTime(timestamp)}");
        }


        protected async void SelectedPipelineChanged(object sender, PipelineModel pipeline)
        {
            await LoadPipelineAsync();
        }

    }

    public record SummaryResult(string Header, string Content, float Score);
}