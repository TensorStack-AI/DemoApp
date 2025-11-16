namespace DemoApp.Common
{
    public class UpscaleItem : HistoryItem
    {
        public string Model { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public int OriginalWidth { get; init; }
        public int OriginalHeight { get; init; }
        public int ScaleFactor { get; init; }
        public UpscaleOptions Options { get;  set; }
    }
}
