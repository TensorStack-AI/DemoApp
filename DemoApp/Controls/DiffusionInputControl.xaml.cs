using DemoApp.Common;
using DemoApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TensorStack.WPF;
using TensorStack.WPF.Controls;

namespace DemoApp.Controls
{
    /// <summary>
    /// Interaction logic for DiffusionInputControl.xaml
    /// </summary>
    public partial class DiffusionInputControl : BaseControl
    {
        private bool _isResolutionEnabled = true;
        private SizeOption _selectedResolution;
        private bool _isImageInputEnabled;
        private bool _isControlNetEnabled;

        public DiffusionInputControl()
        {
            SeedCommand = new RelayCommand<bool>(GenerateSeed);
            DefaultResolutions = [.. Enumerable.Range(4, 24).Select(x => 64 * x)];
            InitializeComponent();
        }

        public static readonly DependencyProperty PipelineProperty = DependencyProperty.Register(nameof(Pipeline), typeof(PipelineModel), typeof(DiffusionInputControl), new PropertyMetadata<DiffusionInputControl, PipelineModel>((c, o, n) => c.OnPipelineChanged(o, n)));
        public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(nameof(Options), typeof(ImageGenerateOptions), typeof(DiffusionInputControl), new PropertyMetadata<DiffusionInputControl, ImageGenerateOptions>((c, o, n) => c.OnOptionsChanged(o, n)));

        public PipelineModel Pipeline
        {
            get { return (PipelineModel)GetValue(PipelineProperty); }
            set { SetValue(PipelineProperty, value); }
        }

        public ImageGenerateOptions Options
        {
            get { return (ImageGenerateOptions)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        public RelayCommand<bool> SeedCommand { get; }
        public List<int> DefaultResolutions { get; }

        public bool IsImageInputEnabled
        {
            get { return _isImageInputEnabled; }
            set { SetProperty(ref _isImageInputEnabled, value); }
        }

        public bool IsControlNetEnabled
        {
            get { return _isControlNetEnabled; }
            set { SetProperty(ref _isControlNetEnabled, value); }
        }

        public bool IsResolutionEnabled
        {
            get { return _isResolutionEnabled; }
            set { SetProperty(ref _isResolutionEnabled, value); }
        }

        public SizeOption SelectedResolution
        {
            get { return _selectedResolution; }
            set
            {
                _selectedResolution = value;
                if (Options != null && _selectedResolution != null)
                {
                    Options.Width = _selectedResolution.Width;
                    Options.Height = _selectedResolution.Height;
                }
                NotifyPropertyChanged();
            }
        }


        private Task OnPipelineChanged(PipelineModel oldPipeline, PipelineModel newPipeline)
        {
            if (newPipeline is null || newPipeline.DiffusionModel is null)
                return Task.CompletedTask;

            var oldModel = oldPipeline?.DiffusionModel;
            var newModel = newPipeline?.DiffusionModel;
            IsControlNetEnabled = newPipeline?.ControlModel != null;
            if (oldModel is not null && oldModel.PipelineType == newModel.PipelineType && oldModel.ModelType == newModel.ModelType)
                return Task.CompletedTask;

            SelectedResolution = newModel.Resolutions.FirstOrDefault(x => x.IsDefault);
            return Task.CompletedTask;
        }


        private Task OnOptionsChanged(ImageGenerateOptions oldOptions, ImageGenerateOptions newOptions)
        {
            if (newOptions is null)
                return Task.CompletedTask;

            if (oldOptions != null)
            {
                newOptions.Seed = oldOptions.Seed;
                newOptions.Prompt = oldOptions.Prompt;
                newOptions.NegativePrompt = oldOptions.NegativePrompt;
            }

            if (IsControlNetEnabled)
            {
                newOptions.Strength = 1f;
            }
            return Task.CompletedTask;
        }
 

        private void GenerateSeed(bool random)
        {
            Options.Seed = random ? 0 : Random.Shared.Next();
        }

    }

}
