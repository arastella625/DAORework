using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;

namespace DAORework.App.Tests;

/// <summary>
/// Saves a rendered window to PNG. The folder comes from DAOREWORK_SCREENSHOT_DIR (CI points it at the uploaded
/// TestResults folder), else the test output folder. Reading that variable is allowed here: tests are not a layer.
/// </summary>
internal static class Screenshots
{
    public static string Directory { get; } =
        Environment.GetEnvironmentVariable("DAOREWORK_SCREENSHOT_DIR") is { Length: > 0 } configured
            ? configured
            : Path.Combine(AppContext.BaseDirectory, "screenshots");

    /// <returns>The saved file's path.</returns>
    public static string Save(Window window, string name)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var path = Path.Combine(Directory, $"{name}.png");
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The headless platform rendered no frame; is Skia configured?");
        frame.Save(path, PngBitmapEncoderOptions.Default);
        return path;
    }
}
