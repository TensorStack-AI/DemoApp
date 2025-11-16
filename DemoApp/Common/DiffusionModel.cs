using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Common;
using TensorStack.StableDiffusion.Enums;
using TensorStack.WPF;
using TensorStack.WPF.Services;

namespace DemoApp.Common
{
    public class DiffusionModel : BaseModel, IModelDownload
    {
        private bool _isValid;

        public int Id { get; init; }
        public string Name { get; init; }
        public bool IsDefault { get; set; }
        public DeviceType[] SupportedDevices { get; init; }
        public ModelType ModelType { get; init; }
        public PipelineType PipelineType { get; init; }
        public bool IsControlNetSupported { get; init; }
        public List<SizeOption> Resolutions { get; init; }
        public string[] UrlPaths { get; init; }


        [JsonIgnore]
        public string Path { get; set; }

        [JsonIgnore]
        public bool IsValid
        {
            get { return _isValid; }
            private set { SetProperty(ref _isValid, value); }
        }

        public void Initialize(string modelDirectory)
        {
            var directory = System.IO.Path.Combine(modelDirectory, Name);
            var modelFiles = FileHelper.GetUrlFileMapping(UrlPaths, directory);
            if (modelFiles.Values.All(File.Exists))
            {
                IsValid = true;
                Path = directory;
            }
        }


        public async Task<bool> DownloadAsync(string modelDirectory)
        {
            var directory = System.IO.Path.Combine(modelDirectory, Name);
            if (await DialogService.DownloadAsync($"Download '{Name}' model?", UrlPaths, directory))
                Initialize(modelDirectory);

            return IsValid;
        }
    }


    public class DiffusionControlNetModel : BaseModel, IModelDownload
    {
        private bool _isValid;

        public int Id { get; init; }
        public string Name { get; init; }
        public bool IsDefault { get; set; }
        public PipelineType PipelineType { get; init; }
        public string[] UrlPaths { get; init; }


        [JsonIgnore]
        public string Path { get; set; }

        [JsonIgnore]
        public bool IsValid
        {
            get { return _isValid; }
            private set { SetProperty(ref _isValid, value); }
        }

        public void Initialize(string modelDirectory)
        {
            var controlDirectory = System.IO.Path.Combine(modelDirectory, PipelineType.ToString(), Name);
            var modelFiles = FileHelper.GetUrlFileMapping(UrlPaths, controlDirectory);
            if (modelFiles.Values.All(File.Exists))
            {
                IsValid = true;
                Path = modelFiles.Values.First(x => x.EndsWith(".onnx"));
            }
        }


        public async Task<bool> DownloadAsync(string modelDirectory)
        {
            var controlDirectory = System.IO.Path.Combine(modelDirectory, PipelineType.ToString(), Name);
            if (await DialogService.DownloadAsync($"Download '{Name}' model?", UrlPaths, controlDirectory))
                Initialize(modelDirectory);

            return IsValid;
        }
    }
}
