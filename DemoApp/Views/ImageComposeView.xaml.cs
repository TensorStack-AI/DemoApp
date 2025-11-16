using System;
using System.Windows.Media.Imaging;
using TensorStack.Image;
using TensorStack.WPF.Services;
using DemoApp.Common;
using DemoApp.Services;

namespace DemoApp.Views
{
    /// <summary>
    /// Interaction logic for ImageComposeView.xaml
    /// </summary>
    public partial class ImageComposeView : ViewBase
    {

        public ImageComposeView(Settings settings, NavigationService navigationService, IHistoryService historyService)
            : base(settings, navigationService, historyService)
        {
            InitializeComponent();
        }

        public override int Id => (int)View.ImageCompose;


        protected async void LayerControl_ImageGenerated(object sender, BitmapSource image)
        {
            var inputImage = new ImageInput(image);
            await HistoryService.AddAsync(inputImage, new LayerImageItem
            {
                Source = View.ImageCompose,
                MediaType = MediaType.Image,
                Model = "Layer",
                Width = inputImage.Width,
                Height = inputImage.Height,
                Timestamp = DateTime.UtcNow
            });
        }

    }
}