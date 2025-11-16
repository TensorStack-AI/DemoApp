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

    public class UpscaleModel : BaseModel, IModelDownload
    {
        private bool _isValid;

        public int Id { get; init; }
        public string Name { get; init; }
        public bool IsDefault { get; set; }
        public DeviceType[] SupportedDevices { get; init; }
        public int Channels { get; init; } = 3;
        public int SampleSize { get; init; }
        public int ScaleFactor { get; init; } = 1;
        public Normalization Normalization { get; init; } = Normalization.ZeroToOne;
        public Normalization OutputNormalization { get; init; } = Normalization.OneToOne;
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


    public class UpscaleOptions : BaseModel
    {
        private TileMode _tileMode = TileMode.ClipBlend;
        private int _tileSize = 512;
        private int _tileOverlap = 16;

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
    }
}
