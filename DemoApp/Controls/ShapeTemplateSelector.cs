using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TensorStack.WPF;

namespace DemoApp.Controls
{
    public class ShapeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LabelTemplate { get; set; }
        public DataTemplate PolygonTemplate { get; set; }
        public DataTemplate RectangleTemplate { get; set; }
        public DataTemplate RectangleGroupTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var shapeItem = item as BorderOverlay;
            if (shapeItem != null)
            {
                switch (shapeItem.Type)
                {
                    case OverlayType.Rectangle:
                        return RectangleTemplate;
                    case OverlayType.RectangleGroup:
                        return RectangleGroupTemplate;
                    case OverlayType.Polygon:
                        return PolygonTemplate;
                    case OverlayType.Label:
                        return LabelTemplate;
                }
            }
            return base.SelectTemplate(item, container);
        }
    }


    public class BorderOverlay : BaseModel
    {
        private bool _isEnabled;

        public int BorderSize { get; set; } = 2;
        public OverlayType Type { get; set; }

        public int Index { get; set; }
        public int Count { get; set; }
        public string Text { get; set; }
        public string ColorHex { get; set; }
        public string TextColorHex { get; set; }

        public BoxPoint Rectangle { get; set; }
        public PointCollection Polygon { get; set; }
        public List<BoxPoint> RectangleGroup { get; set; }


        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProperty(ref _isEnabled, value); }
        }
    }

    public enum OverlayType
    {
        Rectangle = 0,
        RectangleGroup = 1,
        Polygon = 2,
        Label = 3
    }


    public record BoxPoint : BaseRecord
    {
        private float _posX;
        private float _posY;
        private float _width;
        private float _height;

        public BoxPoint(float minX, float minY, float maxX, float maxY)
        {
            PosX = minX;
            PosY = minY;
            Width = maxX - minX;
            Height = maxY - minY;
        }

        public float Width
        {
            get { return _width; }
            set { SetProperty(ref _width, value); }
        }
        public float Height
        {
            get { return _height; }
            set { SetProperty(ref _height, value); }
        }

        public float PosX
        {
            get { return _posX; }
            set { SetProperty(ref _posX, value); }
        }


        public float PosY
        {
            get { return _posY; }
            set { SetProperty(ref _posY, value); }
        }
    }
}
