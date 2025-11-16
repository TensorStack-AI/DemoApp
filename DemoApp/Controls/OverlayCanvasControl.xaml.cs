using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TensorStack.Common;
using TensorStack.TextGeneration.Common;
using TensorStack.TextGeneration.Pipelines.Florence;
using TensorStack.WPF;
using TensorStack.WPF.Controls;

namespace DemoApp.Controls
{
    /// <summary>
    /// Interaction logic for OverlayCanvasControl.xaml
    /// </summary>
    public partial class OverlayCanvasControl : ImageElementBase
    {

        private float _regionX;
        private float _regionY;
        private float _regionW;
        private float _regionH;

        private BorderOverlay _selectedOverlay;

        public OverlayCanvasControl()
        {
            _colors = GetColors();
            Overlays = new ObservableCollection<BorderOverlay>();
            OverlayEnableCommand = new AsyncRelayCommand<BorderOverlay>(OverlayEnableAsync);
            InitializeComponent();
        }


        //public ObservableCollection<BorderOverlay> Overlays
        //{
        //    get { return (ObservableCollection<BorderOverlay>)GetValue(OverlaysProperty); }
        //    set { SetValue(OverlaysProperty, value); }
        //}

        //public static readonly DependencyProperty OverlaysProperty =
        //    DependencyProperty.Register(nameof(Overlays), typeof(ObservableCollection<BorderOverlay>), typeof(OverlayCanvasControl));


        public ObservableCollection<BorderOverlay> Overlays { get; }


        public AsyncRelayCommand<BorderOverlay> OverlayEnableCommand { get; }


        public float RegionX
        {
            get { return _regionX; }
            set { SetProperty(ref _regionX, value); }
        }

        public float RegionY
        {
            get { return _regionY; }
            set { SetProperty(ref _regionY, value); }
        }

        public float RegionW
        {
            get { return _regionW; }
            set { SetProperty(ref _regionW, value); }
        }

        public float RegionH
        {
            get { return _regionH; }
            set { SetProperty(ref _regionH, value); }
        }

        private bool _isTextDocumentMode;

        public bool IsTextDocumentMode
        {
            get { return _isTextDocumentMode; }
            set { SetProperty(ref _isTextDocumentMode, value); }
        }


        public BorderOverlay SelectedOverlay
        {
            get { return _selectedOverlay; }
            set
            {
                if (SetProperty(ref _selectedOverlay, value))
                {
                    if (_selectedOverlay == null)
                        return;

                    if (_selectedOverlay.Rectangle is not null)
                    {
                        RegionX = _selectedOverlay.Rectangle.PosX;
                        RegionY = _selectedOverlay.Rectangle.PosY;
                        RegionW = _selectedOverlay.Rectangle.Width;
                        RegionH = _selectedOverlay.Rectangle.Height;
                    }
                    if (!_selectedOverlay.RectangleGroup.IsNullOrEmpty())
                    {
                        RegionX = _selectedOverlay.RectangleGroup[0].PosX;
                        RegionY = _selectedOverlay.RectangleGroup[0].PosY;
                        RegionW = _selectedOverlay.RectangleGroup[0].Width;
                        RegionH = _selectedOverlay.RectangleGroup[0].Height;
                    }
                }
            }
        }

        public float[] GetSelectedRegion() => [RegionX, RegionY, RegionX + RegionW, RegionY + RegionH];







        public void ResetView()
        {
            Overlays.Clear();
            ClearOverlayMask();
        }















        private Color[] _colors;
        private Dictionary<string, Color> _boxColors = new Dictionary<string, Color>();




        public void AddOverlay(TaskType taskType, GenerateResult result)
        {
            if (result.CoordinateResults.IsNullOrEmpty())
                return;


            var index = 0;
            if (taskType == TaskType.REFERRING_EXPRESSION_SEGMENTATION || taskType == TaskType.REGION_TO_SEGMENTATION)
            {
                foreach (var coordinateResult in result.CoordinateResults)
                {
                    var region = coordinateResult.CoordinateBox;
                    float left = MathF.Min(region.MinX, region.MaxX);
                    float top = MathF.Min(region.MinY, region.MaxY);
                    float right = MathF.Max(region.MinX, region.MaxX);
                    float bottom = MathF.Max(region.MinY, region.MaxY);
                    var color = GetColor(coordinateResult.Label);
                    var label = string.IsNullOrEmpty(coordinateResult.Label) ? $"[{result.Beam}:{result.Score:F4}] Mask #{index + 1}" : $"[{result.Beam}:{result.Score:F4}] {coordinateResult.Label}";
                    Overlays.Add(new BorderOverlay
                    {
                        Index = result.Beam,
                        Count = 1,
                        IsEnabled = true,
                        Text = label,
                        Type = OverlayType.Polygon,
                        Rectangle = new BoxPoint(left, top, right, bottom),
                        Polygon = new PointCollection(coordinateResult.Coordinates.Select(x => new System.Windows.Point(x.PosX, x.PosY)))
                    });
                    index++;
                }
            }
            else if (taskType == TaskType.OCR_WITH_REGION || taskType == TaskType.REGION_TO_OCR)
            {
                foreach (var coordinateResult in result.CoordinateResults)
                {
                    var region = coordinateResult.CoordinateBox;
                    float left = MathF.Min(region.MinX, region.MaxX);
                    float top = MathF.Min(region.MinY, region.MaxY);
                    float right = MathF.Max(region.MinX, region.MaxX);
                    float bottom = MathF.Max(region.MinY, region.MaxY);
                    var color = GetColor(coordinateResult.Label);
                    Overlays.Add(new BorderOverlay
                    {
                        Index = index,
                        Count = 1,
                        IsEnabled = true,
                        ColorHex = color.ToString(),
                        TextColorHex = Colors.White.ToString(),
                        Text = $"[{result.Beam}:{result.Score:F4}] {coordinateResult.Label}",
                        Type = OverlayType.Label,
                        Rectangle = new BoxPoint(left, top, right, bottom),
                        Polygon = new PointCollection(coordinateResult.Coordinates.Select(x => new System.Windows.Point(x.PosX, x.PosY))),
                    });
                    index++;
                }
            }
            else if (taskType == TaskType.REGION_PROPOSAL || taskType == TaskType.CAPTION_TO_PHRASE_GROUNDING)
            {
                foreach (var coordinateResult in result.CoordinateResults)
                {
                    var region = coordinateResult.CoordinateBox;
                    float left = MathF.Min(region.MinX, region.MaxX);
                    float top = MathF.Min(region.MinY, region.MaxY);
                    float right = MathF.Max(region.MinX, region.MaxX);
                    float bottom = MathF.Max(region.MinY, region.MaxY);
                    var label = string.IsNullOrEmpty(coordinateResult.Label) ? $"[{result.Beam}:{result.Score:F4}] Region #{index + 1}" : $"[{result.Beam}:{result.Score:F4}] {coordinateResult.Label}";
                    var color = GetColor(label);
                    Overlays.Add(new BorderOverlay
                    {
                        Index = index,
                        Count = 1,
                        IsEnabled = true,
                        Text = label,
                        ColorHex = color.ToString(),
                        TextColorHex = Colors.White.ToString(),
                        Type = OverlayType.Rectangle,
                        Rectangle = new BoxPoint(left, top, right, bottom),
                        Polygon = new PointCollection(coordinateResult.Coordinates.Select(x => new System.Windows.Point(x.PosX, x.PosY)))
                    });
                    index++;
                }
            }
            else
            {

                foreach (var coordinateResult in result.CoordinateResults.GroupBy(x => x.Label))
                {
                    var label = $"[{result.Beam}:{result.Score:F4}] {coordinateResult.Key}";

                    var color = GetColor(label);

                    Overlays.Add(new BorderOverlay
                    {
                        Index = index,
                        Count = coordinateResult.Count(),
                        IsEnabled = true,
                        Text = label,
                        ColorHex = color.ToString(),
                        TextColorHex = Colors.White.ToString(),
                        Type = OverlayType.RectangleGroup,
                        RectangleGroup = coordinateResult
                            .Select(x => x.CoordinateBox)
                            .Select(coordinate => new BoxPoint(coordinate.MinX, coordinate.MinY, coordinate.MaxX, coordinate.MaxY))
                            .ToList()
                    });

                    index++;
                }
            }

            UpdateOverlayMask();
        }






        private Color GetColor(string key)
        {
            if (_boxColors.ContainsKey(key))
                return _boxColors[key];

            _boxColors[key] = _colors
                .Except(_boxColors.Values)
                .OrderBy(x => Guid.NewGuid())
                .FirstOrDefault(Colors.Red);
            return _boxColors[key];
        }


        private Color[] GetColors()
        {
            return
            [
                Colors.AliceBlue,
                Colors.PaleGoldenrod,
                Colors.Orchid,
                Colors.OrangeRed,
                Colors.Orange,
                Colors.OliveDrab,
                Colors.Olive,
                Colors.OldLace,
                Colors.Navy,
                Colors.NavajoWhite,
                Colors.Moccasin,
                Colors.MistyRose,
                Colors.MintCream,
                Colors.MidnightBlue,
                Colors.MediumVioletRed,
                Colors.MediumTurquoise,
                Colors.MediumSpringGreen,
                Colors.MediumSlateBlue,
                Colors.LightSkyBlue,
                Colors.LightSlateGray,
                Colors.LightSteelBlue,
                Colors.LightYellow,
                Colors.Lime,
                Colors.LimeGreen,
                Colors.PaleGreen,
                Colors.Linen,
                Colors.Maroon,
                Colors.MediumAquamarine,
                Colors.MediumBlue,
                Colors.MediumOrchid,
                Colors.MediumPurple,
                Colors.MediumSeaGreen,
                Colors.Magenta,
                Colors.PaleTurquoise,
                Colors.PaleVioletRed,
                Colors.PapayaWhip,
                Colors.SlateGray,
                Colors.Snow,
                Colors.SpringGreen,
                Colors.SteelBlue,
                Colors.Tan,
                Colors.Teal,
                Colors.SlateBlue,
                Colors.Thistle,
                Colors.Transparent,
                Colors.Turquoise,
                Colors.Violet,
                Colors.Wheat,
                Colors.White,
                Colors.WhiteSmoke,
                Colors.Tomato,
                Colors.LightSeaGreen,
                Colors.SkyBlue,
                Colors.Sienna,
                Colors.PeachPuff,
                Colors.Peru,
                Colors.Pink,
                Colors.Plum,
                Colors.PowderBlue,
                Colors.Purple,
                Colors.Silver,
                Colors.Red,
                Colors.RoyalBlue,
                Colors.SaddleBrown,
                Colors.Salmon,
                Colors.SandyBrown,
                Colors.SeaGreen,
                Colors.SeaShell,
                Colors.RosyBrown,
                Colors.Yellow,
                Colors.LightSalmon,
                Colors.LightGreen,
                Colors.DarkRed,
                Colors.DarkOrchid,
                Colors.DarkOrange,
                Colors.DarkOliveGreen,
                Colors.DarkMagenta,
                Colors.DarkKhaki,
                Colors.DarkGreen,
                Colors.DarkGray,
                Colors.DarkGoldenrod,
                Colors.DarkCyan,
                Colors.DarkBlue,
                Colors.Cyan,
                Colors.Crimson,
                Colors.Cornsilk,
                Colors.CornflowerBlue,
                Colors.Coral,
                Colors.Chocolate,
                Colors.AntiqueWhite,
                Colors.Aqua,
                Colors.Aquamarine,
                Colors.Azure,
                Colors.Beige,
                Colors.Bisque,
                Colors.DarkSalmon,
                Colors.Black,
                Colors.Blue,
                Colors.BlueViolet,
                Colors.Brown,
                Colors.BurlyWood,
                Colors.CadetBlue,
                Colors.Chartreuse,
                Colors.BlanchedAlmond,
                Colors.DarkSeaGreen,
                Colors.DarkSlateBlue,
                Colors.DarkSlateGray,
                Colors.HotPink,
                Colors.IndianRed,
                Colors.Indigo,
                Colors.Ivory,
                Colors.Khaki,
                Colors.Lavender,
                Colors.Honeydew,
                Colors.LavenderBlush,
                Colors.LemonChiffon,
                Colors.LightBlue,
                Colors.LightCoral,
                Colors.LightCyan,
                Colors.LightGoldenrodYellow,
                Colors.LightGray,
                Colors.LawnGreen,
                Colors.LightPink,
                Colors.GreenYellow,
                Colors.Gray,
                Colors.DarkTurquoise,
                Colors.DarkViolet,
                Colors.DeepPink,
                Colors.DeepSkyBlue,
                Colors.DimGray,
                Colors.DodgerBlue,
                Colors.Green,
                Colors.Firebrick,
                Colors.ForestGreen,
                Colors.Fuchsia,
                Colors.Gainsboro,
                Colors.GhostWhite,
                Colors.Gold,
                Colors.Goldenrod,
                Colors.FloralWhite,
                Colors.YellowGreen,
            ];
        }













        private bool _overlayMaskEnabled = true;
        private Geometry _overlayMaskGeometry;
        private double _overlayMaskOpacity = 0.6;


        public bool OverlayMaskEnabled
        {
            get { return _overlayMaskEnabled; }
            set
            {
                if (SetProperty(ref _overlayMaskEnabled, value))
                {
                    UpdateOverlayMask();
                }
            }
        }


        public Geometry OverlayMaskGeometry
        {
            get { return _overlayMaskGeometry; }
            set { SetProperty(ref _overlayMaskGeometry, value); }
        }


        public double OverlayMaskOpacity
        {
            get { return _overlayMaskOpacity; }
            set { SetProperty(ref _overlayMaskOpacity, value); }
        }


        private Task OverlayEnableAsync(BorderOverlay overlay)
        {
            UpdateOverlayMask();
            return Task.CompletedTask;
        }


        private void ClearOverlayMask()
        {
            OverlayMaskGeometry = null;
        }

        private void UpdateOverlayMask()
        {
            if (Source is null || Overlays.IsNullOrEmpty() || !_overlayMaskEnabled)
            {
                ClearOverlayMask();
                return;
            }

            var rectanglesGeometry = new GeometryGroup
            {
                FillRule = FillRule.Nonzero
            };

            foreach (var overlay in Overlays)
            {
                if (!overlay.IsEnabled)
                    continue;
                if (overlay.Type == OverlayType.Polygon)
                {
                    var start = overlay.Polygon.First();
                    var p = new PolyLineSegment(overlay.Polygon, true);

                    var poly = new PathFigure(new System.Windows.Point(start.X, start.Y), [p], true);
                    rectanglesGeometry.Children.Add(new System.Windows.Media.PathGeometry([poly]));
                    continue;
                }
                else if (overlay.Type == OverlayType.RectangleGroup)
                {
                    foreach (var box in overlay.RectangleGroup)
                    {
                        var rect = new Rect(Math.Max(0, box.PosX), Math.Max(0, box.PosY), Math.Max(0, box.Width), Math.Max(0, box.Height));
                        rectanglesGeometry.Children.Add(new RectangleGeometry(rect));
                    }
                    continue;
                }
                else
                {
                    var rect = new Rect(Math.Max(0, overlay.Rectangle.PosX), Math.Max(0, overlay.Rectangle.PosY), Math.Max(0, overlay.Rectangle.Width), Math.Max(0, overlay.Rectangle.Height));
                    rectanglesGeometry.Children.Add(new RectangleGeometry(rect));
                }
            }

            var maskRectangle = new Rect(0, 0, Source.Width, Source.Height);
            var maskGeometry = new RectangleGeometry(maskRectangle);
            OverlayMaskGeometry = new CombinedGeometry(GeometryCombineMode.Exclude, maskGeometry, rectanglesGeometry);
        }




        public BitmapSource SaveCanvas()
        {
            var width = Source.Width;
            var height = Source.Height;
            var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            Surface.Measure(new Size(width, height));
            Surface.Arrange(new Rect(new Size(width, height)));
            Surface.UpdateLayout();
            renderBitmap.Render(Surface);
            return renderBitmap;
        }









        private System.Windows.Point _startPoint;
        private bool _isSelecting;










        private void SelectionCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(SelectionCanvas); // Get the mouse starting position
            _isSelecting = true;

            // Reset and show the selection rectangle
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            SelectionRectangle.Visibility = Visibility.Visible;
        }


        private void SelectionCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;

            var currentPoint = e.GetPosition(SelectionCanvas);

            // Calculate the new size and position of the selection rectangle
            double x = Math.Min(currentPoint.X, _startPoint.X);
            double y = Math.Min(currentPoint.Y, _startPoint.Y);

            double width = Math.Abs(currentPoint.X - _startPoint.X);
            double height = Math.Abs(currentPoint.Y - _startPoint.Y);

            // Update the rectangle's size and position
            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }


        private void SelectionCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SelectRegion();
        }


        private void SelectionCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            SelectRegion();
        }


        private void SelectRegion()
        {
            _isSelecting = false;
            RegionX = (float)Canvas.GetLeft(SelectionRectangle);
            RegionY = (float)Canvas.GetTop(SelectionRectangle);
            RegionW = (float)SelectionRectangle.ActualWidth;
            RegionH = (float)SelectionRectangle.ActualHeight;
        }


        public void ClearSelection()
        {
            SelectionRectangle.Visibility = Visibility.Collapsed;
        }
    }
}
