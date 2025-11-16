using System.Text.Json.Serialization;

namespace DemoApp.Common
{
    public class DiffusionItem : HistoryItem
    {
        public string Model { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Control { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Extractor { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Upscaler { get; init; }
    }
}
