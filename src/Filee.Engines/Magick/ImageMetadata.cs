// What a picture carries besides its pixels: the EXIF tags a camera or a phone wrote, the XMP and IPTC blocks an
// editor added, and the colour profile.
//
// This is for the metadata tool, which shows the user what is actually in their file before they decide to drop it.
// Saying "metadata will be removed" without naming it is asking them to trust a checkbox; the GPS line of a holiday
// photo is the one that makes the point.

using ImageMagick;

namespace Filee.Engines.Magick;

/// <summary>One piece of metadata, as the tool lists it.</summary>
/// <param name="Group">EXIF, XMP, IPTC or ICC.</param>
/// <param name="Name">Tag name, already readable ("DateTimeOriginal", "GPSLatitude").</param>
/// <param name="Value">Its value, shortened when it is a long blob.</param>
public sealed record MetadataEntry(string Group, string Name, string Value);

/// <summary>Reads the metadata blocks of a picture.</summary>
public static class ImageMetadata
{
    /// <summary>Longest value shown in full; past this the tool shows the beginning and says it is longer.</summary>
    private const int MaxValue = 120;

    /// <summary>
    /// Every tag found, EXIF first. An empty list means the file carries nothing, which is itself worth showing.
    /// Returns an empty list for a file ImageMagick cannot read, rather than throwing into a tool window.
    /// </summary>
    public static IReadOnlyList<MetadataEntry> Read(string file)
    {
        try
        {
            using var image = new MagickImage(file);
            var entries = new List<MetadataEntry>();

            if (image.GetExifProfile() is { } exif)
                entries.AddRange(exif.Values
                    .Select(v => new MetadataEntry("EXIF", v.Tag.ToString(), Shorten(v.GetValue()?.ToString())))
                    .Where(e => e.Value.Length > 0));

            if (image.GetXmpProfile() is { } xmp)
                entries.Add(new MetadataEntry("XMP", "XMP", $"{xmp.ToByteArray().Length} o"));

            if (image.GetIptcProfile() is { } iptc)
                entries.AddRange(iptc.Values
                    .Select(v => new MetadataEntry("IPTC", v.Tag.ToString(), Shorten(v.Value)))
                    .Where(e => e.Value.Length > 0));

            if (image.GetColorProfile() is { } icc)
                entries.Add(new MetadataEntry("ICC", icc.Name ?? "ICC", $"{icc.ToByteArray().Length} o"));

            return entries;
        }
        catch (Exception e) when (e is MagickException or IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static string Shorten(string? value)
    {
        value = value?.Replace('\n', ' ').Replace('\r', ' ').Trim() ?? "";
        return value.Length <= MaxValue ? value : value[..MaxValue] + "…";
    }
}
