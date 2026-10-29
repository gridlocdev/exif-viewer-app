using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ExifViewer.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            if (desktop.Args is [var path, ..])
                window.Open(Path.GetFullPath(path));
            desktop.MainWindow = window;
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void OnAboutClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } owner })
            new AboutWindow().ShowDialog(owner);
    }
}
