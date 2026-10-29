using ExifViewer.Core.Containers;

namespace ExifViewer.Core;

/// <summary>Collects directories, warnings and preview candidates while the container readers run.</summary>
internal sealed class ReportBuilder
{
    private readonly List<MetadataDirectory> directories = [];
    private readonly List<string> warnings = [];
    private ReadOnlyMemory<byte>? preview;

    public int? Orientation { get; set; }

    /// <summary>Returns the directory with this name, creating it at the end of the list if needed.</summary>
    public MetadataDirectory GetDirectory(string name)
    {
        var existing = directories.FirstOrDefault(d => d.Name == name);
        if (existing is not null)
            return existing;
        var created = new MetadataDirectory(name);
        directories.Add(created);
        return created;
    }

    /// <summary>A name not used by any directory yet: <paramref name="name"/>, then "name 2", "name 3"…</summary>
    public string UniqueName(string name)
    {
        if (directories.All(d => d.Name != name))
            return name;
        for (int i = 2; ; i++)
        {
            string candidate = $"{name} {i}";
            if (directories.All(d => d.Name != candidate))
                return candidate;
        }
    }

    public void Add(string directory, string name, string value, int? id = null) =>
        GetDirectory(directory).Tags.Add(new MetadataTag(name, value, id));

    public void Warn(string message) => warnings.Add(message);

    /// <summary>Keeps <paramref name="jpeg"/> as the preview if it can be shown and is larger than the current one.</summary>
    public void OfferPreview(ReadOnlyMemory<byte> jpeg)
    {
        if (JpegReader.IsDisplayable(jpeg.Span) && (preview is null || jpeg.Length > preview.Value.Length))
            preview = jpeg;
    }

    public MetadataReport Build(string fileName, string format) => new()
    {
        FileName = fileName,
        Format = format,
        Directories = directories.Where(d => d.Tags.Count > 0).ToList(),
        Warnings = warnings,
        Preview = preview,
        Orientation = Orientation,
    };
}
