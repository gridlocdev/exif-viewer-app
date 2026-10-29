using System.Reflection;
using Avalonia.Controls;

namespace ExifViewer.App;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {Version}";
    }

    /// <summary>The csproj Version, without the "+commit" suffix the SDK appends.</summary>
    public static string Version =>
        typeof(AboutWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0] ?? "unknown";
}
