using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ExifViewer.Core;

namespace ExifViewer.App;

/// <summary>One directory as shown in the list, after filtering.</summary>
public sealed record TagGroup(string Name, IReadOnlyList<MetadataTag> Tags)
{
    public string Title => $"{Name} ({Tags.Count})";
}

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType Images = new("Images")
    {
        Patterns =
        [
            "*.jpg", "*.jpeg", "*.png", "*.webp", "*.tif", "*.tiff",
            "*.dng", "*.cr2", "*.nef", "*.nrw", "*.arw", "*.orf", "*.rw2", "*.pef",
        ],
    };

    private MetadataReport? report;

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

    private void ShowPreview(string path, MetadataReport metadata)
    {
        (PreviewImage.Source as IDisposable)?.Dispose();
        // The file itself when the platform decoder handles it (JPEG, PNG, WebP), else the embedded JPEG.
        var bitmap = TryDecode(() => new Bitmap(path))
            ?? (metadata.Preview is { } jpeg ? TryDecode(() => new Bitmap(new MemoryStream(jpeg.ToArray()))) : null);
        PreviewImage.Source = bitmap;
        PreviewPlaceholder.IsVisible = bitmap is null;
        PreviewTransform.LayoutTransform = metadata.Orientation switch
        {
            3 => new RotateTransform(180),
            6 => new RotateTransform(90),
            8 => new RotateTransform(270),
            _ => null,
        };
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
            FileTypeFilter = [Images, FilePickerFileTypes.All],
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
