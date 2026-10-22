// The retouching editor's state: light, contrast, colour and sharpness, shown on the real picture.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Filee.Core.Presets;
using Filee.Engines.Magick;

namespace Filee.App.ViewModels;

/// <summary>Light, contrast, colour and sharpness.</summary>
public sealed partial class RetouchToolViewModel : ObservableObject, IDisposable
{
    private readonly ImageOptions _options;

    public RetouchToolViewModel(ImagePreview source, ImageOptions options, string targetFormat, string fileName)
    {
        _options = options.Clone();
        FileName = fileName;
        Preview = new ToolPreview(source, targetFormat);
        _brightness = options.Brightness;
        _contrast = options.Contrast;
        _saturation = options.Saturation;
        _sharpen = options.Sharpen;
        Apply();
    }

    public string FileName { get; }
    public ToolPreview Preview { get; }

    [ObservableProperty]
    private int _brightness;

    [ObservableProperty]
    private int _contrast;

    [ObservableProperty]
    private int _saturation;

    [ObservableProperty]
    private int _sharpen;

    /// <summary>True while every knob rests where it started: nothing would change.</summary>
    public bool IsUntouched => Brightness == 0 && Contrast == 0 && Saturation == 0 && Sharpen == 0;

    partial void OnBrightnessChanged(int value) { _ = value; Apply(); }
    partial void OnContrastChanged(int value) { _ = value; Apply(); }
    partial void OnSaturationChanged(int value) { _ = value; Apply(); }
    partial void OnSharpenChanged(int value) { _ = value; Apply(); }

    private void Apply()
    {
        _options.Brightness = Math.Clamp(Brightness, -100, 100);
        _options.Contrast = Math.Clamp(Contrast, -100, 100);
        _options.Saturation = Math.Clamp(Saturation, -100, 100);
        _options.Sharpen = Math.Clamp(Sharpen, 0, 100);
        OnPropertyChanged(nameof(IsUntouched));
        Preview.Refresh(_options);
    }

    /// <summary>Writes the settings into the preset the conversion will run.</summary>
    public void WriteInto(Preset preset)
    {
        preset.Image.Brightness = _options.Brightness;
        preset.Image.Contrast = _options.Contrast;
        preset.Image.Saturation = _options.Saturation;
        preset.Image.Sharpen = _options.Sharpen;
    }

    [RelayCommand]
    private void Reset()
    {
        Brightness = 0;
        Contrast = 0;
        Saturation = 0;
        Sharpen = 0;
    }

    public void Dispose() => Preview.Dispose();
}
