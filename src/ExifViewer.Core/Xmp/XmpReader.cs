using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ExifViewer.Core.Xmp;

/// <summary>
/// Flattens an XMP packet into "prefix:Name" tags. Each top-level rdf:Description contributes its
/// attributes and child properties; arrays (Seq, Bag, Alt) are joined and structures are listed as "field: value".
/// </summary>
internal static class XmpReader
{
    private static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    public static void Read(ReadOnlySpan<byte> packet, ReportBuilder report)
    {
        string xml = Encoding.UTF8.GetString(packet).Trim('\0', '﻿', ' ', '\r', '\n', '\t');
        XDocument doc;
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            doc = XDocument.Load(reader);
        }
        catch (XmlException e)
        {
            report.Warn("XMP: " + e.Message);
            return;
        }

        var directory = report.GetDirectory("XMP");
        var descriptions = doc.Descendants(Rdf + "Description").Where(d => d.Parent?.Name == Rdf + "RDF");
        foreach (var description in descriptions)
        {
            foreach (var attribute in description.Attributes())
                if (!attribute.IsNamespaceDeclaration && attribute.Name.Namespace != Rdf)
                    directory.Tags.Add(new MetadataTag(Name(attribute.Name, description), attribute.Value));
            foreach (var property in description.Elements())
                directory.Tags.Add(new MetadataTag(Name(property.Name, property), ValueOf(property)));
        }
    }

    private static string Name(XName name, XElement scope) =>
        $"{scope.GetPrefixOfNamespace(name.Namespace) ?? name.NamespaceName}:{name.LocalName}";

    private static string ValueOf(XElement property)
    {
        var array = property.Elements().FirstOrDefault(e => e.Name == Rdf + "Seq" || e.Name == Rdf + "Bag" || e.Name == Rdf + "Alt");
        if (array is not null)
            return string.Join(", ", array.Elements(Rdf + "li").Select(ValueOf));
        if (property.Attribute(Rdf + "resource") is { } resource)
            return resource.Value;

        var fields = Fields(property).ToList();
        return fields.Count > 0 ? string.Join("; ", fields) : property.Value.Trim();
    }

    /// <summary>Structure fields, whether written as child elements or as attributes of a nested rdf:Description.</summary>
    private static IEnumerable<string> Fields(XElement element)
    {
        foreach (var attribute in element.Attributes())
            if (!attribute.IsNamespaceDeclaration && attribute.Name.Namespace != Rdf && attribute.Name.Namespace != XNamespace.Xml)
                yield return $"{attribute.Name.LocalName}: {attribute.Value}";
        foreach (var child in element.Elements())
        {
            if (child.Name == Rdf + "Description")
            {
                foreach (var field in Fields(child))
                    yield return field;
            }
            else
            {
                yield return $"{child.Name.LocalName}: {ValueOf(child)}";
            }
        }
    }
}
