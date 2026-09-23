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

    // The full, unfiltered set of cards for whatever group is currently shown; Cards is what's actually
    // bound to the view and gets narrowed down from this whenever SearchText changes.
    private List<ToolCardViewModel> _allCards = [];

    [ObservableProperty]
    private string _groupKey = NavGroupKeys.Home;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public HomeViewModel(ILocalizationService loc, Action<ToolDescriptor> openTool, Func<ToolDescriptor, object> pageFactory)
        : base(loc)
    {
        _openTool = openTool;
        _pageFactory = pageFactory;
        Cards = [];
    }

    public ObservableCollection<ToolCardViewModel> Cards { get; }

    /// <summary>The search box only makes sense on the App Center's "everything" view — other groups
    /// already show a short, curated list where filtering would be more friction than it's worth.</summary>
    public bool ShowSearch => GroupKey == NavGroupKeys.Home;

    public string SearchPlaceholder => Loc.Translate("Home.SearchPlaceholder");

    /// <summary>The Home nav group has no tools of its own assigned to it (it exists purely as a
    /// landing page), so unlike every other group — which shows only its own tools — Home shows every
    /// available tool across all groups as a simple overview/dashboard, matching the "app center"
    /// concept from the H_Assistant reference project.</summary>
    public void ShowGroup(string groupKey)
    {
        GroupKey = groupKey;
        SearchText = string.Empty;

        var descriptors = groupKey == NavGroupKeys.Home
            ? ToolCatalog.All.Where(d => d.IsAvailable)
            : ToolCatalog.ForGroup(groupKey);
        _allCards = descriptors
            .Select(d => new ToolCardViewModel(d, Loc, QuickToggleTargetFor(d)))
            .ToList();

        ApplyFilter();
        OnPropertyChanged(nameof(ShowSearch));
    }

    private object? QuickToggleTargetFor(ToolDescriptor descriptor) =>
        descriptor.IsAvailable && QuickToggleToolIds.Contains(descriptor.Id) ? _pageFactory(descriptor) : null;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var matches = string.IsNullOrWhiteSpace(SearchText)
            ? _allCards
            : _allCards.Where(c => c.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));

        Cards.Clear();
        foreach (var card in matches)
        {
            Cards.Add(card);
        }
    }

    [RelayCommand]
    private void OpenTool(ToolCardViewModel card) => _openTool(card.Descriptor);

    protected override void OnLanguageChanged() => OnPropertyChanged(nameof(SearchPlaceholder));
}
