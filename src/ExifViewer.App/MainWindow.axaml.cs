using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ExifViewer.Core;

namespace ExifViewer.App;

/// <summary>One directory as shown in the list, after filtering.</summary>
public sealed record TagGroup(string Name, IReadOnlyList<MetadataTag> Tags);

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType Images = new("Images")
    {
        Patterns =
        [
            "*.jpg", "*.jpeg", "*.png", "*.webp", "*.tif", "*.tiff",
            "*.dng", "*.cr2", "*.nef", "*.nrw", "*.arw", "*.orf", "*.rw2", "*.pef",
            "*.heic", "*.heif", "*.avif",
        ],
    };

    private static readonly FilePickerFileType Videos = new("Videos")
    {
        Patterns = ["*.mov", "*.mp4", "*.m4v", "*.3gp"],
    };

    private const int SystemPreviewSize = 1600;

    private MetadataReport? report;
    // Bumped on every open, so a slow Quick Look render for an earlier file is dropped.
    private int previewVersion;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    public void Open(string path)
    {
        try
        {
            report = MetadataReader.Read(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = $"Couldn't open {Path.GetFileName(path)}: {e.Message}";
            return;
        }

        Title = $"{report.FileName} — EXIF Viewer";
        FileTitle.Text = report.FileName;
        ShowPreview(path, report);
        ShowTags();
        int tagCount = report.Directories.Sum(d => d.Tags.Count);
        StatusText.Text = report.Warnings.Count == 0
            ? $"{report.Format} · {tagCount} tags in {report.Directories.Count} groups"
            : $"{report.Format} · {tagCount} tags · " + string.Join(" · ", report.Warnings);
    }

    /// <summary>
    /// The file itself when the platform decoder handles it (JPEG, PNG, WebP), else the embedded JPEG,
    /// else (macOS only) a Quick Look thumbnail rendered in the background.
    /// </summary>
    private async void ShowPreview(string path, MetadataReport metadata)
    {
        int version = ++previewVersion;
        var bitmap = TryDecode(() => new Bitmap(path))
            ?? (metadata.Preview is { } jpeg ? TryDecode(() => new Bitmap(new MemoryStream(jpeg.ToArray()))) : null);
        if (bitmap is not null || !SystemPreview.IsAvailable)
        {
            SetPreview(bitmap, metadata.Orientation, $"No preview for {metadata.Format}");
            return;
        }

        SetPreview(null, null, "Rendering preview…");
        var png = await SystemPreview.RenderAsync(path, SystemPreviewSize);
        if (version != previewVersion)
            return;
        bitmap = png is null ? null : TryDecode(() => new Bitmap(new MemoryStream(png)));
        // Quick Look applies the orientation itself.
        SetPreview(bitmap, null, $"No preview for {metadata.Format}");
    }

    private void SetPreview(Bitmap? bitmap, int? orientation, string placeholder)
    {
        (PreviewImage.Source as IDisposable)?.Dispose();
        PreviewImage.Source = bitmap;
        PreviewPlaceholder.IsVisible = bitmap is null;
        PreviewPlaceholder.Text = placeholder;
        PreviewTransform.LayoutTransform = UprightTransform(orientation);
    }

    /// <summary>What turns an image with this EXIF orientation right-side up: a mirror, a clockwise rotation, or both.</summary>
    private static Transform? UprightTransform(int? orientation)
    {
        (bool mirror, double rotate) = orientation switch
        {
            2 => (true, 0),
            3 => (false, 180),
            4 => (true, 180),
            5 => (true, 270),
            6 => (false, 90),
            7 => (true, 90),
            8 => (false, 270),
            _ => (false, 0.0),
        };
        if (!mirror)
            return rotate == 0 ? null : new RotateTransform(rotate);
        var group = new TransformGroup();
        group.Children.Add(new ScaleTransform(-1, 1));
        if (rotate != 0)
            group.Children.Add(new RotateTransform(rotate));
        return group;
    }

    private static Bitmap? TryDecode(Func<Bitmap> decode)
    {
        try
        {
            return decode();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void ShowTags()
    {
        if (report is null)
            return;
        string filter = FilterBox.Text?.Trim() ?? "";
        bool Matches(string text) => text.Contains(filter, StringComparison.OrdinalIgnoreCase);
        GroupList.ItemsSource = report.Directories
            .Select(d => new TagGroup(d.Name, Matches(d.Name) ? d.Tags : d.Tags.Where(t => Matches(t.Name) || Matches(t.Value)).ToList()))
            .Where(g => g.Tags.Count > 0)
            .ToList();
    }

    private void OnFilterChanged(object? sender, TextChangedEventArgs e) => ShowTags();

    private async void OnOpenClick(object? sender, RoutedEventArgs e) => await PickFile();

    private async Task PickFile()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open image",
            AllowMultiple = false,
            FileTypeFilter = [Images, Videos, FilePickerFileTypes.All],
        });
        if (files.Count > 0 && files[0].TryGetLocalPath() is string path)
            Open(path);
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.O && e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta)
        {
            e.Handled = true;
            await PickFile();
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e) =>
        e.DragEffects = e.DataTransfer.TryGetFile() is not null ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.TryGetFile()?.TryGetLocalPath() is string path)
            Open(path);
    }
}
