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

    // ── 卡片右键菜单文字 ──
    // 直接绑定在卡片自身上：ContextMenu 会继承所附着按钮的 DataContext（即本卡片），
    // 不依赖 #Root 名称作用域，也不依赖 Avalonia 12 中不会自动赋值的 ContextMenu.PlacementTarget
    // （之前在 Opened 事件里通过 PlacementTarget 赋值，结果拿到 null 直接返回，菜单文字全部为空）。

    /// <summary>"置顶" / "取消置顶"，随 <see cref="IsPinned"/> 切换。</summary>
    public string PinMenuHeader => Loc.Translate(IsPinned ? "Home.Unpin" : "Home.PinToTop");

    public string EditMenuHeader => Loc.Translate("Home.Edit");

    public string DeleteMenuHeader => Loc.Translate("Home.Delete");

    partial void OnIsPinnedChanged(bool value) => OnPropertyChanged(nameof(PinMenuHeader));

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(PinMenuHeader));
        OnPropertyChanged(nameof(EditMenuHeader));
        OnPropertyChanged(nameof(DeleteMenuHeader));
    }
}
