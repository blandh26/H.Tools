using Avalonia.Media;
using Avalonia.Media.Imaging;
using HTools.Core.Models;
using HTools.App.Models;
using HTools.App.Services;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed partial class ToolCardViewModel : LocalizedViewModelBase
{
    private readonly CustomToolItem? _customTool;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool _isPinned;

    public ToolCardViewModel(ToolDescriptor descriptor, ILocalizationService loc, object? quickToggleTarget = null)
        : base(loc)
    {
        Descriptor = descriptor;
        QuickToggleTarget = quickToggleTarget;
        IconImage = AppIcons.TryGet(descriptor.Id);
    }

    public ToolCardViewModel(CustomToolItem customTool, ILocalizationService loc, bool isPinned)
        : base(loc)
    {
        _customTool = customTool;
        IsPinned = isPinned;
        if (!string.IsNullOrWhiteSpace(customTool.IconPngBase64))
        {
            try
            {
                using var stream = new MemoryStream(Convert.FromBase64String(customTool.IconPngBase64));
                IconImage = new Bitmap(stream);
            }
            catch
            {
                IconImage = null;
            }
        }
    }

    public ToolDescriptor? Descriptor { get; }

    public string Id => Descriptor?.Id ?? _customTool!.Id;

    public bool IsCustom => _customTool is not null;

    public CustomToolItem? CustomTool => _customTool;

    public string Target => _customTool?.Target ?? string.Empty;

    /// <summary>When set, this is a live view model exposing a bool <c>IsEnabled</c> property; the
    /// card shows an inline toggle bound straight to it, so flipping it here or on the tool's own
    /// page stays in sync (same instance either way).</summary>
    public object? QuickToggleTarget { get; }

    /// <summary>A colorful SVG icon for this tool, if <see cref="AppIcons"/> has a custom one; null
    /// means the card should fall back to the plain emoji glyph in <see cref="Icon"/> instead.</summary>
    public IImage? IconImage { get; }

    public string Icon => Descriptor?.Icon ?? (_customTool?.Kind == "url" ? "🌐" : "🧩");

    public string Name => Descriptor is null ? _customTool!.Name : Loc.Translate(Descriptor.NameKey);

    public string Description => Descriptor is null ? _customTool!.Target : Loc.Translate(Descriptor.DescriptionKey);

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
    }
}
