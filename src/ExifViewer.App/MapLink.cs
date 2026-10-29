using System.Globalization;
using ExifViewer.Core;

namespace ExifViewer.App;

/// <summary>
/// Where "Show on map" goes: Apple Maps on macOS (the link opens the Maps app), OpenStreetMap in the
/// browser elsewhere. Windows' own Maps app is retired and Linux has no standard one.
/// </summary>
internal static class MapLink
{
    public static string ServiceName => OperatingSystem.IsMacOS() ? "Apple Maps" : "OpenStreetMap";

    public static Uri For(GeoPosition position)
    {
        string lat = position.Latitude.ToString("0.######", CultureInfo.InvariantCulture);
        string lon = position.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
        return OperatingSystem.IsMacOS()
            ? new Uri($"https://maps.apple.com/?ll={lat},{lon}&q={lat},{lon}")
            : new Uri($"https://www.openstreetmap.org/?mlat={lat}&mlon={lon}#map=16/{lat}/{lon}");
    }
}
