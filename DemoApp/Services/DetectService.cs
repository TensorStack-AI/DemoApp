using DemoApp.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.Common.Vision;
using TensorStack.Providers;
using TensorStack.TextGeneration.Common;
using TensorStack.TextGeneration.Pipelines.Florence;

namespace DemoApp.Services
{
    public class DetectService : ServiceBase, IDetectService
    {
        private readonly Settings _settings;
        private PipelineModel _currentPipeline;
        private FlorencePipeline _detectPipeline;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetectService"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public DetectService(Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        public PipelineModel Pipeline => _currentPipeline;

        /// <summary>
        /// Gets a value indicating whether this instance is loaded.
        /// </summary>
        public bool IsLoaded
        {
            get { return _isLoaded; }
            private set { SetProperty(ref _isLoaded, value); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is loading.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            private set { SetProperty(ref _isLoading, value); NotifyPropertyChanged(nameof(CanCancel)); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is executing.
        /// </summary>
        public bool IsExecuting
        {
            get { return _isExecuting; }
            private set { SetProperty(ref _isExecuting, value); NotifyPropertyChanged(nameof(CanCancel)); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can cancel.
        /// </summary>
        public bool CanCancel => _isLoading || _isExecuting;


        /// <summary>
        /// Load the Detect pipeline
        /// </summary>
        /// <param name="config">The configuration.</param>
        public async Task LoadAsync(PipelineModel pipeline)
        {
            try
            {
                IsLoaded = false;
                IsLoading = true;
                using (_cancellationTokenSource = new CancellationTokenSource())
                {
                    var cancellationToken = _cancellationTokenSource.Token;
                    if (_detectPipeline != null)
                        await _detectPipeline.UnloadAsync(cancellationToken);

                    _currentPipeline = pipeline;
                    var model = _currentPipeline.DetectModel;
                    var provider = _currentPipeline.Device.GetProvider();
                    var providerCPU = Provider.GetProvider(DeviceType.CPU); // TODO: DirectML not working with decoder
                    _detectPipeline = FlorencePipeline.Create(provider, providerCPU, provider, provider, model.Path, model.Type);
                    await Task.Run(() => _detectPipeline.LoadAsync(cancellationToken), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _detectPipeline?.Dispose();
                _detectPipeline = null;
                _currentPipeline = null;
                throw;
            }
            finally
            {
                IsLoaded = true;
                IsLoading = false;
            }
        }


        /// <summary>
        /// Execute the Detector
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<GenerateResult[]> ExecuteAsync(DetectImageRequest options)
        {
            try
            {
                IsExecuting = true;
                using (_cancellationTokenSource = new CancellationTokenSource())
                {
                    var pipelineOptions = new FlorenceOptions
                    {
                        Prompt = options.Prompt,
                        Seed = options.Seed,
                        Beams = options.Beams,
                        TopK = options.TopK,
                        TopP = options.TopP,
                        Temperature = options.Temperature,
                        MaxLength = options.MaxLength,
                        MinLength = options.MinLength,
                        NoRepeatNgramSize = options.NoRepeatNgramSize,
                        LengthPenalty = options.LengthPenalty,
                        DiversityLength = options.DiversityLength,
                        EarlyStopping = options.EarlyStopping,

                        // Florence
                        Image = options.Image,
                        TaskType = options.TaskType,
                        Region = new CoordinateBox<float>(options.Region)
                    };


                    var pipelineResult = await Task.Run(async () =>
                    {
                        if (options.Beams == 0)
                        {
                            // Greedy Search
                            return [await _detectPipeline.RunAsync(pipelineOptions, cancellationToken: _cancellationTokenSource.Token)];
                        }

                        // Beam Search
                        return await _detectPipeline.RunAsync(new FlorenceSearchOptions(pipelineOptions), cancellationToken: _cancellationTokenSource.Token);
                    });

                    return pipelineResult;
                }
            }
            finally
            {
                IsExecuting = false;
            }
        }


        /// <summary>
        /// Cancel the running task (Load or Execute)
        /// </summary>
        public async Task CancelAsync()
        {
            await _cancellationTokenSource.SafeCancelAsync();
        }


        /// <summary>
        /// Unload the pipeline
        /// </summary>
        public async Task UnloadAsync()
        {
            if (_detectPipeline != null)
            {
                await _cancellationTokenSource.SafeCancelAsync();
                await _detectPipeline.UnloadAsync();
                _detectPipeline.Dispose();
                _detectPipeline = null;
                _currentPipeline = null;
            }

            IsLoaded = false;
            IsLoading = false;
            IsExecuting = false;
        }
    }


    public interface IDetectService
    {
        PipelineModel Pipeline { get; }
        bool IsLoaded { get; }
        bool IsLoading { get; }
        bool IsExecuting { get; }
        bool CanCancel { get; }
        Task LoadAsync(PipelineModel pipeline);
        Task UnloadAsync();
        Task CancelAsync();
        Task<GenerateResult[]> ExecuteAsync(DetectImageRequest options);
    }


    public record DetectImageRequest : ITransformerRequest
    {
        public TaskType TaskType { get; set; }
        public ImageTensor Image { get; set; }
        public float[] Region { get; set; }


        public string Prompt { get; set; }
        public int MinLength { get; set; } = 20;
        public int MaxLength { get; set; } = 200;
        public int NoRepeatNgramSize { get; set; } = 3;
        public int Seed { get; set; }
        public int Beams { get; set; } = 1;
        public int TopK { get; set; } = 1;
        public float TopP { get; set; } = 0.9f;
        public float Temperature { get; set; } = 1.0f;
        public float LengthPenalty { get; set; } = 1.0f;
        public EarlyStopping EarlyStopping { get; set; }
        public int DiversityLength { get; set; } = 5;
    }

}
