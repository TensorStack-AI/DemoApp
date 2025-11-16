using System;
using System.Text.Json.Serialization;
using DemoApp.Views;

namespace DemoApp.Common
{
    public abstract class HistoryItem
    {
        public string Id { get; set; }
        public View Source { get; init; }
        public MediaType MediaType { get; init; }
        public DateTime Timestamp { get; init; }
        public string Extension { get; set; }

        [JsonIgnore]
        public string FilePath { get; set; }

        [JsonIgnore]
        public string MediaPath { get; set; }

        [JsonIgnore]
        public string ThumbPath { get; set; }
    }
}
