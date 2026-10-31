using Avalonia;

namespace DAORework.App;

/// <summary>Entry point (SDD §9.2). The composition root lives in <see cref="App"/>, which runs once Avalonia is up.</summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Also used by the Avalonia designer.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
