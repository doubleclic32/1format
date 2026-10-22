// What a picture would weigh and look like with a given set of options, without writing a file.
//
// The tools that let the user turn a knob (compression, retouching) need the true answer, not an estimate: the image
// is really encoded, into memory, with exactly the code the conversion runs. A guess from a formula would drift from
// what the file ends up being, and the number on screen would be a lie.
//
// Encoding a 50-megapixel photo takes a moment, so the picture is read once into a Session the window keeps, and only
// the encoding is redone as the knob moves.

using Filee.Core.Presets;
using ImageMagick;

namespace Filee.Engines.Magick;

/// <summary>A picture encoded with some options: its bytes and the size it came out at.</summary>
public sealed record EncodedImage(byte[] Bytes, int Width, int Height)
{
    public long Length => Bytes.LongLength;
}

/// <summary>Encodes a picture in memory so a tool can show what its settings would really produce.</summary>
public sealed class ImagePreview : IDisposable
{
    private readonly MagickImage _source;

    private ImagePreview(MagickImage source, string format, long originalLength)
    {
        _source = source;
        SourceFormat = format;
        OriginalLength = originalLength;
        OriginalWidth = (int)source.Width;
        OriginalHeight = (int)source.Height;
    }

    public string SourceFormat { get; }
    public long OriginalLength { get; }
    public int OriginalWidth { get; }
    public int OriginalHeight { get; }

    /// <summary>
    /// Reads <paramref name="file"/> once. Null when ImageMagick cannot read it, so the caller can fall back to
    /// converting without a preview rather than failing.
    /// </summary>
    public static ImagePreview? Open(string file, string sourceFormat)
    {
        try
        {
            var image = new MagickImage(file);
            // Phone photos are stored sideways with an orientation tag: the preview must show them the right way up,
            // like ApplyOptions does, or every size and crop the user picks is against the wrong picture.
            image.AutoOrient();
            image.Orientation = OrientationType.TopLeft;
            return new ImagePreview(image, sourceFormat, new FileInfo(file).Length);
        }
        catch (Exception e) when (e is MagickException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// The picture encoded as <paramref name="targetFormat"/> with <paramref name="options"/>, exactly as the
    /// conversion would write it. Returns null when ImageMagick refuses the combination.
    /// </summary>
    public EncodedImage? Encode(ImageOptions options, string targetFormat)
    {
        try
        {
            using var copy = (MagickImage)_source.Clone();
            ImageEncoder.ApplyOptions(copy, options, targetFormat);
            using var stream = new MemoryStream();
            copy.Write(stream, ImageEncoder.ToMagickFormat(targetFormat));
            return new EncodedImage(stream.ToArray(), (int)copy.Width, (int)copy.Height);
        }
        catch (MagickException)
        {
            return null;
        }
    }

    public void Dispose() => _source.Dispose();
}
