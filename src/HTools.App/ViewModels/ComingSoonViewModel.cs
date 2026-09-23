using HTools.App.Models;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed class ComingSoonViewModel : LocalizedViewModelBase
{
    private readonly ToolDescriptor _descriptor;

    public ComingSoonViewModel(ToolDescriptor descriptor, ILocalizationService loc)
        : base(loc)
    {
        _descriptor = descriptor;
    }

    public string Icon => _descriptor.Icon;

    public string Name => Loc.Translate(_descriptor.NameKey);

    public string Message => Loc.Translate("Common.ComingSoon");

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Message));
    }
}
