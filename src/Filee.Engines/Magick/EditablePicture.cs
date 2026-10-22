// The picture an editor must show so that what the user draws lands where they drew it.
//
// ApplyOptions straightens the picture (AutoOrient) and then turns it as the preset says, and only after that does it
// cut, redact and annotate. The rectangles those three tools produce are therefore in the pixels of the straightened,
// turned picture.
//
// An editor that loaded the file directly would show the picture as it is stored. On a phone photo, stored sideways
// with an orientation tag, the two differ by a quarter turn: the rectangle drawn around a bank detail lands outside
// the picture, Cut drops it without a word and Redact clamps it to the edge. The file goes out unmasked, and the
// conversion reports success.

using Filee.Core.Presets;
using ImageMagick;

namespace Filee.Engines.Magick;

/// <summary>Loads a picture the way the engine will see it just before cutting, redacting or annotating.</summary>
public static class EditablePicture
{
    /// <summary>
    /// PNG bytes of <paramref name="file"/> straightened and turned as <paramref name="options"/> say, or null when
    /// ImageMagick cannot read it. PNG rather than raw pixels so the caller gets a picture it can simply open, with
    /// no buffer to keep alive.
    /// </summary>
    public static byte[]? Load(string file, ImageOptions options)
    {
        try
        {
            using var image = new MagickImage(file);
            image.AutoOrient();
            image.Orientation = OrientationType.TopLeft;
            ImageEncoder.Turn(image, options);
            using var stream = new MemoryStream();
            image.Write(stream, MagickFormat.Png);
            return stream.ToArray();
        }
        catch (Exception e) when (e is MagickException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
