using System.Threading.Tasks;

namespace DemoApp.Common
{
    public interface IModelDownload
    {
        string Name { get; init; }
        string[] UrlPaths { get; init; }
        string Path { get; set; }
        bool IsValid { get;  }
        void Initialize(string modelDirectory);
        Task<bool> DownloadAsync(string modelDirectory);
    }
}
