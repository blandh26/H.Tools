using CommunityToolkit.Mvvm.ComponentModel;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed partial class NavGroupViewModel : LocalizedViewModelBase
{
    private readonly string _nameKey;

    public NavGroupViewModel(string key, string icon, string nameKey, ILocalizationService loc)
        : base(loc)
    {
        Key = key;
        Icon = icon;
        _nameKey = nameKey;
    }

    public string Key { get; }

    public string Icon { get; }

    public string Name => Loc.Translate(_nameKey);

    [ObservableProperty]
    private bool _isSelected;

    protected override void OnLanguageChanged() => OnPropertyChanged(nameof(Name));
}
