using HTools.App.Models;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed class ToolCardViewModel : LocalizedViewModelBase
{
    public ToolCardViewModel(ToolDescriptor descriptor, ILocalizationService loc, object? quickToggleTarget = null)
        : base(loc)
    {
        Descriptor = descriptor;
        QuickToggleTarget = quickToggleTarget;
    }

    public ToolDescriptor Descriptor { get; }

    /// <summary>When set, this is a live view model exposing a bool <c>IsEnabled</c> property; the
    /// card shows an inline toggle bound straight to it, so flipping it here or on the tool's own
    /// page stays in sync (same instance either way).</summary>
    public object? QuickToggleTarget { get; }

    public string Icon => Descriptor.Icon;

    public string Name => Loc.Translate(Descriptor.NameKey);

    public string Description => Loc.Translate(Descriptor.DescriptionKey);

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
    }
}
