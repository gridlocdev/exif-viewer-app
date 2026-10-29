using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace ExifViewer.App;

/// <summary>
/// macOS only: asks Quick Look (<c>qlmanage</c>) for a thumbnail. That covers HEIC, video, TIFF and RAW
/// without bundling a codec. Thumbnails come back with rotation already applied. Other platforms get null.
/// </summary>
internal static class SystemPreview
{
    private const string QuickLook = "/usr/bin/qlmanage";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    public static bool IsAvailable => OperatingSystem.IsMacOS() && File.Exists(QuickLook);

    /// <summary>PNG bytes no larger than <paramref name="size"/> pixels on the long side, or null if Quick Look can't render the file.</summary>
    public static async Task<byte[]?> RenderAsync(string path, int size)
    {
        if (!IsAvailable)
            return null;
        string outputDir = Path.Combine(Path.GetTempPath(), "exif-viewer-preview-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDir);
        try
        {
            var start = new ProcessStartInfo(QuickLook)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (string arg in new[] { "-t", "-s", size.ToString(CultureInfo.InvariantCulture), "-o", outputDir, path })
                start.ArgumentList.Add(arg);

            using var process = Process.Start(start);
            if (process is null)
                return null;
            using var timeout = new CancellationTokenSource(Timeout);
            try
            {
                await Task.WhenAll(
                    process.StandardOutput.ReadToEndAsync(timeout.Token),
                    process.StandardError.ReadToEndAsync(timeout.Token),
                    process.WaitForExitAsync(timeout.Token));
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            string? png = Directory.EnumerateFiles(outputDir, "*.png").FirstOrDefault();
            return png is null ? null : await File.ReadAllBytesAsync(png);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
        {
            return null;
        }
        finally
        {
            try
            {
                Directory.Delete(outputDir, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
