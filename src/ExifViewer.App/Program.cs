using Avalonia;

namespace ExifViewer.App;

internal static class Program
{
    // Don't touch Avalonia APIs before AppMain: the platform isn't initialized yet.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Also used by the XAML previewer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
