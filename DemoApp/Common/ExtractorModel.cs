using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Common;
using TensorStack.WPF;
using TensorStack.WPF.Services;

namespace DemoApp.Common
{
    public class ExtractorModel : BaseModel, IModelDownload
    {
        private bool _isValid;

        public int Id { get; init; }
        public string Name { get; init; }
        public bool IsDefault { get; set; }
        public DeviceType[] SupportedDevices { get; init; }
        public ExtractorType Type { get; init; }
        public int Channels { get; init; }
        public int SampleSize { get; init; }
        public Normalization Normalization { get; init; } = Normalization.ZeroToOne;
        public Normalization OutputNormalization { get; init; } = Normalization.OneToOne;
        public int OutputChannels { get; init; }
        public bool IsDynamicOutput { get; init; }
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
                Path = modelFiles.Values.First(x => x.EndsWith(".onnx"));
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

    public enum ExtractorType
    {
        Default = 0,
        Background = 1,
        Pose = 2
    }
}
