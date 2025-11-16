using DemoApp.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using TensorStack.Common;
using TensorStack.Common.Common;
using TensorStack.StableDiffusion.Enums;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace DemoApp.Controls
{
    /// <summary>
    /// Interaction logic for DiffusionModelControl.xaml
    /// </summary>
    public partial class DiffusionModelControl : BaseControl
    {
        private ListCollectionView _modelCollectionView;
        private ListCollectionView _controlNetCollectionView;
        private ListCollectionView _extractorCollectionView;
        private ListCollectionView _upscaleCollectionView;
        private Device _selectedDevice;
        private DiffusionModel _selectedModel;
        private DiffusionControlNetModel _selectedControlNet;
        private UpscaleModel _selectedUpscaler;
        private ExtractorModel _selectedExtractor;
        private bool _isControlNetSupported;
        private bool _isExtractorSupported;
        private bool _isUpscalerSupported;
        private bool _isControlNetEnabled;
        private bool _isExtractorEnabled;
        private bool _isUpscalerEnabled;
        private Device _currentDevice;
        private DiffusionModel _currentModel;
        private DiffusionControlNetModel _currentControlNet;
        private ExtractorModel _currentExtractor;
        private UpscaleModel _currentUpscaler;
        private bool _currentControlNetEnabled;
        private bool _currentUpscalerEnabled;
        private bool _currentExtractorEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiffusionModelControl"/> class.
        /// </summary>
        public DiffusionModelControl()
        {
            LoadCommand = new AsyncRelayCommand(LoadAsync, CanLoad);
            UnloadCommand = new AsyncRelayCommand(UnloadAsync, CanUnload);
            InitializeComponent();
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(Settings), typeof(DiffusionModelControl), new PropertyMetadata<DiffusionModelControl>((c) => c.OnSettingsChanged()));
        public static readonly DependencyProperty IsSelectionValidProperty = DependencyProperty.Register(nameof(IsSelectionValid), typeof(bool), typeof(DiffusionModelControl));


        public PipelineModel CurrentPipeline
        {
            get { return (PipelineModel)GetValue(CurrentPipelineProperty); }
            set { SetValue(CurrentPipelineProperty, value); }
        }


        public static readonly DependencyProperty CurrentPipelineProperty =
            DependencyProperty.Register(nameof(CurrentPipeline), typeof(PipelineModel), typeof(DiffusionModelControl));



        public event EventHandler<PipelineModel> SelectionChanged;

        public AsyncRelayCommand LoadCommand { get; }
        public AsyncRelayCommand UnloadCommand { get; }

        public Settings Settings
        {
            get { return (Settings)GetValue(SettingsProperty); }
            set { SetValue(SettingsProperty, value); }
        }

        public bool IsSelectionValid
        {
            get { return (bool)GetValue(IsSelectionValidProperty); }
            set { SetValue(IsSelectionValidProperty, value); }
        }

        public Device SelectedDevice
        {
            get { return _selectedDevice; }
            set { SetProperty(ref _selectedDevice, value); }
        }

        public DiffusionModel SelectedModel
        {
            get { return _selectedModel; }
            set { SetProperty(ref _selectedModel, value); }
        }

        public DiffusionControlNetModel SelectedControlNet
        {
            get { return _selectedControlNet; }
            set { SetProperty(ref _selectedControlNet, value); }
        }

        public ExtractorModel SelectedExtractor
        {
            get { return _selectedExtractor; }
            set { SetProperty(ref _selectedExtractor, value); }
        }

        public UpscaleModel SelectedUpscaler
        {
            get { return _selectedUpscaler; }
            set { SetProperty(ref _selectedUpscaler, value); }
        }

        public ListCollectionView ModelCollectionView
        {
            get { return _modelCollectionView; }
            set { SetProperty(ref _modelCollectionView, value); }
        }

        public ListCollectionView ControlNetCollectionView
        {
            get { return _controlNetCollectionView; }
            set { SetProperty(ref _controlNetCollectionView, value); }
        }

        public ListCollectionView UpscaleCollectionView
        {
            get { return _upscaleCollectionView; }
            set { SetProperty(ref _upscaleCollectionView, value); }
        }

        public ListCollectionView ExtractorCollectionView
        {
            get { return _extractorCollectionView; }
            set { SetProperty(ref _extractorCollectionView, value); }
        }

        public bool IsControlNetSupported
        {
            get { return _isControlNetSupported; }
            set { SetProperty(ref _isControlNetSupported, value); }
        }

        public bool IsExtractorSupported
        {
            get { return _isExtractorSupported; }
            set { SetProperty(ref _isExtractorSupported, value); }
        }

        public bool IsUpscalerSupported
        {
            get { return _isUpscalerSupported; }
            set { SetProperty(ref _isUpscalerSupported, value); }
        }

        public bool IsControlNetEnabled
        {
            get { return _isControlNetEnabled; }
            set { SetProperty(ref _isControlNetEnabled, value); }
        }

        public bool IsExtractorEnabled
        {
            get { return _isExtractorEnabled; }
            set { SetProperty(ref _isExtractorEnabled, value); }
        }

        public bool IsUpscalerEnabled
        {
            get { return _isUpscalerEnabled; }
            set { SetProperty(ref _isUpscalerEnabled, value); }
        }

        private async Task LoadAsync()
        {
            if (!await IsPipelineValidAsync())
                return;

            _currentDevice = SelectedDevice;
            _currentModel = SelectedModel;
            _currentControlNet = SelectedControlNet;
            _currentExtractor = SelectedExtractor;
            _currentUpscaler = SelectedUpscaler;
            _currentControlNetEnabled = _isControlNetEnabled;
            _currentExtractorEnabled = _isExtractorEnabled;
            _currentUpscalerEnabled = _isUpscalerEnabled;

            CurrentPipeline = new PipelineModel
            {
                Device = _currentDevice,
                DiffusionModel = _currentModel,
                ControlModel = _currentControlNetEnabled ? _currentControlNet : default,
                ExtractorModel = _currentExtractorEnabled ? _currentExtractor : default,
                UpscaleModel = _currentUpscalerEnabled ? _currentUpscaler : default,
            };

            SelectionChanged?.Invoke(this, CurrentPipeline);
        }


        private bool CanLoad()
        {
            var isReloadRequired = SelectedDevice is not null
                && SelectedModel is not null
                && (!IsControlNetEnabled || SelectedControlNet is not null)
                && (!IsExtractorEnabled || SelectedExtractor is not null)
                && (!IsUpscalerEnabled || SelectedUpscaler is not null)
                && HasCurrentChanged();

            var isSelectionValid = !isReloadRequired;
            if (IsSelectionValid != isSelectionValid)
                IsSelectionValid = isSelectionValid;

            return isReloadRequired;
        }


        private Task UnloadAsync()
        {
            _currentModel = default;

            SelectedControlNet = default;
            IsControlNetEnabled = false;
            _currentControlNet = default;
            _currentControlNetEnabled = false;

            SelectedExtractor = default;
            IsExtractorEnabled = false;
            _currentExtractor = default;
            _currentExtractorEnabled = false;

            SelectedUpscaler = default;
            IsUpscalerEnabled = false;
            _currentUpscaler = default;
            _currentUpscalerEnabled = false;

            CurrentPipeline = new PipelineModel
            {
                Device = _selectedDevice
            };

            SelectionChanged?.Invoke(this, CurrentPipeline);
            Model_SelectionChanged(default, default);
            return Task.CompletedTask;
        }


        private bool CanUnload()
        {
            return _currentModel is not null
                || _currentControlNet is not null
                || _currentExtractor is not null
                || _currentUpscaler is not null;
        }


        private bool HasCurrentChanged()
        {
            return _currentDevice != SelectedDevice
                || _currentModel != SelectedModel
                || _currentControlNet != SelectedControlNet
                || _currentExtractor != SelectedExtractor
                || _currentUpscaler != SelectedUpscaler
                || _currentControlNetEnabled != _isControlNetEnabled
                || _currentExtractorEnabled != _isExtractorEnabled
                || _currentUpscalerEnabled != _isUpscalerEnabled;
        }


        private Task OnSettingsChanged()
        {
            // Base Models
            ModelCollectionView = new ListCollectionView(Settings.DiffusionModels);
            ModelCollectionView.Filter = (obj) =>
            {
                if (obj is not DiffusionModel viewModel)
                    return false;

                if (_selectedDevice == null)
                    return false;

                return viewModel.SupportedDevices?.Contains(_selectedDevice.Type) ?? false;
            };

            //ControlNet models
            ControlNetCollectionView = new ListCollectionView(Settings.DiffusionControlNetModels);
            ControlNetCollectionView.Filter = (obj) =>
            {
                if (obj is not DiffusionControlNetModel viewModel)
                    return false;

                if (SelectedModel is null)
                    return false;

                if (!SelectedModel.IsControlNetSupported)
                    return false;

                if (SelectedModel.PipelineType == PipelineType.LatentConsistency)
                    return viewModel.PipelineType == PipelineType.StableDiffusion;

                return viewModel.PipelineType == SelectedModel.PipelineType;
            };

            //Extractor models
            ExtractorCollectionView = new ListCollectionView(Settings.ExtractorModels);
            ExtractorCollectionView.Filter = (obj) =>
            {
                if (obj is not ExtractorModel viewModel)
                    return false;

                if (SelectedModel is null)
                    return false;

                return viewModel.SupportedDevices?.Contains(_selectedDevice.Type) ?? false;
            };

            //Upscale models
            UpscaleCollectionView = new ListCollectionView(Settings.UpscaleModels);
            UpscaleCollectionView.Filter = (obj) =>
            {
                if (obj is not UpscaleModel viewModel)
                    return false;

                if (SelectedModel is null)
                    return false;

                return viewModel.SupportedDevices?.Contains(_selectedDevice.Type) ?? false;
            };

            SelectedDevice = Settings.DefaultDevice;
            return Task.CompletedTask;
        }


        private void Device_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ModelCollectionView?.Refresh();
            if (ModelCollectionView is not null)
            {
                SelectedModel = ModelCollectionView.Cast<DiffusionModel>().FirstOrDefault(x => x == _currentModel || x.IsDefault);
            }
        }


        private void Model_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ControlNetCollectionView is not null)
            {
                ControlNetCollectionView.Refresh();
                SelectedControlNet = ControlNetCollectionView.Cast<DiffusionControlNetModel>().FirstOrDefault(x => x == _currentControlNet || x.IsDefault);
            }

            if (ExtractorCollectionView is not null)
            {
                ExtractorCollectionView.Refresh();
                SelectedExtractor = ExtractorCollectionView.Cast<ExtractorModel>().FirstOrDefault(x => x == _currentExtractor || x.IsDefault);
            }

            if (UpscaleCollectionView is not null)
            {
                UpscaleCollectionView.Refresh();
                SelectedUpscaler = UpscaleCollectionView.Cast<UpscaleModel>().FirstOrDefault(x => x == _currentUpscaler || x.IsDefault);
            }
        }


        private async Task<bool> IsPipelineValidAsync()
        {
            if (_selectedModel is not null && !_selectedModel.IsValid)
            {
                if (!await _selectedModel.DownloadAsync(Path.Combine(Settings.DirectoryModel, "Diffusion")))
                    return false;
            }

            if (_isControlNetSupported && _isControlNetEnabled && _selectedControlNet is not null && !_selectedControlNet.IsValid)
            {
                if (!await _selectedControlNet.DownloadAsync(Path.Combine(Settings.DirectoryModel, "Control")))
                    return false;
            }

            if (_isExtractorSupported && _isExtractorEnabled && _selectedExtractor is not null && !_selectedExtractor.IsValid)
            {
                if (!await _selectedExtractor.DownloadAsync(Path.Combine(Settings.DirectoryModel, "Extractor")))
                    return false;
            }

            if (_isUpscalerSupported && _isUpscalerEnabled && _selectedUpscaler is not null && !_selectedUpscaler.IsValid)
            {
                if (!await _selectedUpscaler.DownloadAsync(Path.Combine(Settings.DirectoryModel, "Upscale")))
                    return false;
            }

            return true;
        }
    }
}
