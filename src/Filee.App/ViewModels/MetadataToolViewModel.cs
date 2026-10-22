// The metadata editor's state: what the file carries, and what is kept.

using CommunityToolkit.Mvvm.ComponentModel;
using Filee.Core.Presets;
using Filee.Engines.Magick;

namespace Filee.App.ViewModels;

/// <summary>Shows the metadata of a picture and lets the user decide what survives the conversion.</summary>
public sealed partial class MetadataToolViewModel : ObservableObject
{
    public MetadataToolViewModel(IReadOnlyList<MetadataEntry> entries, ImageOptions options, string fileName)
    {
        Entries = entries;
        FileName = fileName;
        _keepMetadata = options.KeepMetadata;
        _keepColorProfile = options.KeepColorProfile;
    }

    public IReadOnlyList<MetadataEntry> Entries { get; }
    public string FileName { get; }

    public bool HasEntries => Entries.Count > 0;
    public int Count => Entries.Count;

    [ObservableProperty]
    private bool _keepMetadata;

    /// <summary>
    /// Only means anything once the rest is dropped: with everything kept, the colour profile is kept with it.
    /// Without it, wide-gamut photos come out washed out, which is why it is a question of its own.
    /// </summary>
    [ObservableProperty]
    private bool _keepColorProfile;

    public bool CanChooseColorProfile => !KeepMetadata;

    partial void OnKeepMetadataChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(CanChooseColorProfile));
    }

    /// <summary>Writes the choice into the preset the conversion will run.</summary>
    public void WriteInto(Preset preset)
    {
        preset.Image.KeepMetadata = KeepMetadata;
        preset.Image.KeepColorProfile = KeepColorProfile;
    }
}
