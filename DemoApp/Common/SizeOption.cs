using System.Text.Json.Serialization;

namespace DemoApp.Common
{
    public record SizeOption
    {
        public int Width { get; init; }
        public int Height { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsDefault { get; init; }


        [JsonIgnore]
        public AspectType Aspect
        {
            get
            {
                if (Width > Height)
                    return AspectType.Landscape;
                else if (Height > Width)
                    return AspectType.Portrait;
                return AspectType.Square;
            }
        }

        public override string ToString()
        {
            return $"{Width} x {Height}";
        }
    }

    public enum AspectType
    {
        Square = 0,
        Portrait = 1,
        Landscape = 2,
    }
}
