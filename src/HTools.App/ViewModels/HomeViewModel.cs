using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Models;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed partial class HomeViewModel : LocalizedViewModelBase
{
    // Tools whose card shows an inline on/off toggle wired straight to their live page view model.
    private static readonly HashSet<string> QuickToggleToolIds = ["mouse-effect"];

    private readonly Action<ToolDescriptor> _openTool;
    private readonly Func<ToolDescriptor, object> _pageFactory;

    [ObservableProperty]
    private string _groupKey = NavGroupKeys.Home;

    public HomeViewModel(ILocalizationService loc, Action<ToolDescriptor> openTool, Func<ToolDescriptor, object> pageFactory)
        : base(loc)
    {
        _openTool = openTool;
        _pageFactory = pageFactory;
        Cards = [];
        RecentTools = [];
    }

    public ObservableCollection<ToolCardViewModel> Cards { get; }

    public ObservableCollection<ToolCardViewModel> RecentTools { get; }

    public bool HasRecentTools => RecentTools.Count > 0;

    public string RecentlyUsedLabel => Loc.Translate("Home.RecentlyUsed");

    public string NoRecentItemsLabel => Loc.Translate("Home.NoRecentItems");

    public void ShowGroup(string groupKey)
    {
        GroupKey = groupKey;
        Cards.Clear();
        foreach (var descriptor in ToolCatalog.ForGroup(groupKey))
        {
            Cards.Add(new ToolCardViewModel(descriptor, Loc, QuickToggleTargetFor(descriptor)));
        }
    }

    public void SetRecentTools(IEnumerable<ToolDescriptor> descriptors)
    {
        RecentTools.Clear();
        foreach (var descriptor in descriptors)
        {
            RecentTools.Add(new ToolCardViewModel(descriptor, Loc, QuickToggleTargetFor(descriptor)));
        }

        OnPropertyChanged(nameof(HasRecentTools));
    }

    private object? QuickToggleTargetFor(ToolDescriptor descriptor) =>
        descriptor.IsAvailable && QuickToggleToolIds.Contains(descriptor.Id) ? _pageFactory(descriptor) : null;

    [RelayCommand]
    private void OpenTool(ToolCardViewModel card) => _openTool(card.Descriptor);

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(RecentlyUsedLabel));
        OnPropertyChanged(nameof(NoRecentItemsLabel));
    }
}
