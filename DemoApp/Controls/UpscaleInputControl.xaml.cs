using DemoApp.Common;
using DemoApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TensorStack.Common;
using TensorStack.WPF;
using TensorStack.WPF.Controls;

namespace DemoApp.Controls
{
    /// <summary>
    /// Interaction logic for UpscaleInputControl.xaml
    /// </summary>
    public partial class UpscaleInputControl : BaseControl
    {
        public UpscaleInputControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty PipelineProperty = DependencyProperty.Register(nameof(Pipeline), typeof(PipelineModel), typeof(UpscaleInputControl), new PropertyMetadata<UpscaleInputControl, PipelineModel>((c, o, n) => c.OnPipelineChanged(o, n)));
        public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(nameof(Options), typeof(UpscaleOptions), typeof(UpscaleInputControl), new PropertyMetadata<UpscaleInputControl, UpscaleOptions>((c, o, n) => c.OnOptionsChanged(o, n)));

        public PipelineModel Pipeline
        {
            get { return (PipelineModel)GetValue(PipelineProperty); }
            set { SetValue(PipelineProperty, value); }
        }

        public UpscaleOptions Options
        {
            get { return (UpscaleOptions)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }


        private Task OnPipelineChanged(PipelineModel oldPipeline, PipelineModel newPipeline)
        {
            return Task.CompletedTask;
        }


        private Task OnOptionsChanged(UpscaleOptions oldOptions, UpscaleOptions newOptions)
        {
            return Task.CompletedTask;
        }

    }

}
