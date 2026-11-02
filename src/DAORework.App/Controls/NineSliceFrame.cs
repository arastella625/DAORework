using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace DAORework.App.Controls;

/// <summary>
/// Draws a frame from a white border image (ADR-0026 §5: Kenney's <i>Fantasy UI Borders</i>) at any size, painted in a
/// theme brush: the corners keep their shape and the edges stretch between them. The middle is left empty.
/// </summary>
public sealed class NineSliceFrame : Control
{
    public static readonly StyledProperty<Bitmap?> SourceProperty = AvaloniaProperty.Register<NineSliceFrame, Bitmap?>(nameof(Source));

    public static readonly StyledProperty<IBrush?> BrushProperty = AvaloniaProperty.Register<NineSliceFrame, IBrush?>(nameof(Brush));

    /// <summary>Defines how far the corners reach into the image, in its pixels.</summary>
    public static readonly StyledProperty<double> SliceProperty = AvaloniaProperty.Register<NineSliceFrame, double>(nameof(Slice), 16);

    /// <summary>Defines how large the image's pixels are drawn: 0.5 draws a 4-pixel line 2 pixels wide.</summary>
    public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<NineSliceFrame, double>(nameof(Scale), 0.5);

    static NineSliceFrame()
    {
        AffectsRender<NineSliceFrame>(SourceProperty, BrushProperty, SliceProperty, ScaleProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<NineSliceFrame>(false);
    }

    public Bitmap? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public double Slice
    {
        get => GetValue(SliceProperty);
        set => SetValue(SliceProperty, value);
    }

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Source is not { } image || Brush is not { } brush)
        {
            return;
        }

        var size = image.Size;
        var slice = Slice;
        var drawn = slice * Scale;
        double[] fromX = [0, slice, size.Width - slice, size.Width];
        double[] fromY = [0, slice, size.Height - slice, size.Height];
        double[] toX = [0, drawn, Bounds.Width - drawn, Bounds.Width];
        double[] toY = [0, drawn, Bounds.Height - drawn, Bounds.Height];
        for (var column = 0; column < 3; column++)
        {
            for (var row = 0; row < 3; row++)
            {
                var target = new Rect(toX[column], toY[row], toX[column + 1] - toX[column], toY[row + 1] - toY[row]);
                if ((column == 1 && row == 1) || target.Width <= 0 || target.Height <= 0)
                {
                    continue;
                }

                // The image's alpha in that part decides where the brush shows, so any theme color paints it.
                var source = new Rect(fromX[column], fromY[row], fromX[column + 1] - fromX[column], fromY[row + 1] - fromY[row]);
                var mask = new ImageBrush(image) { SourceRect = new RelativeRect(source, RelativeUnit.Absolute), Stretch = Stretch.Fill };
                using (context.PushOpacityMask(mask, target))
                {
                    context.FillRectangle(brush, target);
                }
            }
        }
    }
}
