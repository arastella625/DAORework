using Avalonia;
using Avalonia.Headless;
using DAORework.App.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace DAORework.App.Tests;

/// <summary>The headless host renders real frames through Skia so the page tests can save screenshots (SDD §10).</summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<DAORework.App.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
