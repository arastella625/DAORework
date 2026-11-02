using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DAORework.App.Controls;

/// <summary>
/// A soft burst of rays in a theme brush (ADR-0026 §4), behind <i>Ready to play</i>: our own drawing, so every theme and
/// custom look recolors it. The rays fade in from the middle and out at the edge.
/// </summary>
public sealed class Sunburst : Control
{
    public static readonly StyledProperty<IBrush?> BrushProperty = AvaloniaProperty.Register<Sunburst, IBrush?>(nameof(Brush));

    public static readonly StyledProperty<int> RaysProperty = AvaloniaProperty.Register<Sunburst, int>(nameof(Rays), 24);

    static Sunburst()
    {
        AffectsRender<Sunburst>(BrushProperty, RaysProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<Sunburst>(false);
    }

    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public int Rays
    {
        get => GetValue(RaysProperty);
        set => SetValue(RaysProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Brush is not { } brush || Rays < 1)
        {
            return;
        }

        var bounds = new Rect(Bounds.Size);
        var center = bounds.Center;
        var radius = Math.Min(bounds.Width, bounds.Height) / 2;
        var half = Math.PI / Rays * 0.45;
        var rays = new StreamGeometry();
        using (var draw = rays.Open())
        {
            for (var ray = 0; ray < Rays; ray++)
            {
                var angle = 2 * Math.PI * ray / Rays;
                draw.BeginFigure(center, isFilled: true);
                draw.LineTo(center + (new Vector(Math.Cos(angle - half), Math.Sin(angle - half)) * radius));
                draw.LineTo(center + (new Vector(Math.Cos(angle + half), Math.Sin(angle + half)) * radius));
                draw.EndFigure(isClosed: true);
            }
        }

        var fade = new RadialGradientBrush
        {
            GradientStops = { new GradientStop(Colors.Transparent, 0.12), new GradientStop(Colors.Black, 0.3), new GradientStop(Colors.Transparent, 1) },
        };
        using (context.PushOpacityMask(fade, bounds))
        {
            context.DrawGeometry(brush, null, rays);
        }
    }
}
