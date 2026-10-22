// The compression editor's state: the quality and the scale, with the weight of the result measured for real.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Filee.Core.Presets;
using Filee.Engines.Magick;

namespace Filee.App.ViewModels;

/// <summary>Make the file lighter: quality, and scale for the formats where quality alone does little.</summary>
public sealed partial class CompressToolViewModel : ObservableObject, IDisposable
{
    private readonly ImageOptions _options;

    /// <summary>The preset as it came in, so an untouched scale can be handed back exactly as it was.</summary>
    private readonly ImageOptions _original;

    public CompressToolViewModel(ImagePreview source, ImageOptions options, string targetFormat, string fileName)
    {
        _options = options.Clone();
        _original = options.Clone();
        FileName = fileName;
        Preview = new ToolPreview(source, targetFormat);
        _quality = Math.Clamp(options.Quality, 1, 100);
        _scale = 100;
        Apply();
    }

    public string FileName { get; }
    public ToolPreview Preview { get; }

    [ObservableProperty]
    private int _quality;

    /// <summary>Percent of the original width and height; 100 keeps the picture as it is.</summary>
    [ObservableProperty]
    private int _scale;

    public string OriginalSize => ToolPreview.Human(Preview.OriginalLength);

    partial void OnQualityChanged(int value)
    {
        _ = value;
        Apply();
    }

    partial void OnScaleChanged(int value)
    {
        _ = value;
        Apply();
    }

    private void Apply()
    {
        _options.Quality = Math.Clamp(Quality, 1, 100);
        if (Scale >= 100)
        {
            // Left where the preset had it. Forcing None here threw away a preset's own "1600 px wide" for anyone
            // who opened the tool only to move the quality.
            _options.Resize = _original.Resize;
            _options.ResizePercent = _original.ResizePercent;
        }
        else
        {
            // Percent is the one mode that means something for a picture of any shape, and "half the size" is what
            // the user is after here.
            _options.Resize = ResizeMode.Percent;
            _options.ResizePercent = Math.Clamp(Scale, 1, 100);
        }
        Preview.Refresh(_options);
    }

    /// <summary>Writes the settings into the preset the conversion will run.</summary>
    public void WriteInto(Preset preset)
    {
        preset.Image.Quality = _options.Quality;
        preset.Image.Resize = _options.Resize;
        preset.Image.ResizePercent = _options.ResizePercent;
    }

    [RelayCommand]
    private void Reset()
    {
        Quality = 90;
        Scale = 100;
    }

    public void Dispose() => Preview.Dispose();
}
