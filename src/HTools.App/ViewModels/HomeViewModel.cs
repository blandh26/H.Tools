using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Models;
using HTools.App.Services;
using HTools.Core.Models;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed partial class HomeViewModel : LocalizedViewModelBase
{
    private static readonly HashSet<string> QuickToggleToolIds = ["mouse-effect"];
    private readonly Action<ToolDescriptor> _openTool;
    private readonly Func<ToolDescriptor, object> _pageFactory;
    private readonly SettingsViewModel _settingsPage;
    private readonly AppSettingsContext _settings;
    private List<ToolCardViewModel> _allCards = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    public HomeViewModel(ILocalizationService loc, Action<ToolDescriptor> openTool, Func<ToolDescriptor, object> pageFactory, SettingsViewModel settingsPage)
        : base(loc)
    {
        _openTool = openTool;
        _pageFactory = pageFactory;
        _settingsPage = settingsPage;
        _settings = settingsPage.SettingsContext;
        _settingsPage.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.MinToolCardWidth))
            {
                OnPropertyChanged(nameof(CardMinWidth));
            }
        };
        Cards = [];
        RefreshTools();
    }

    public ObservableCollection<ToolCardViewModel> Cards { get; }

    public double CardMinWidth => _settingsPage.MinToolCardWidth;

    public bool ShowSearch => true;

    public string SearchPlaceholder => Loc.Translate("Home.SearchPlaceholder");

    public string DragToSortTooltip => Loc.Translate("Home.DragToSort");

    public string PinnedTooltip => Loc.Translate("Home.Pinned");

    /// <summary>Handed to the add/edit/delete dialogs, which are created from HomeView's code-behind
    /// and so have no other path to the localization service.</summary>
    public ILocalizationService Localization => Loc;

    private void RefreshTools()
    {
        var pinned = _settings.Current.PinnedToolIds.ToHashSet(StringComparer.Ordinal);
        _allCards = ToolCatalog.All.Where(x => x.IsAvailable)
            .Select(x => new ToolCardViewModel(x, Loc, QuickToggleTargetFor(x)) { IsPinned = pinned.Contains(x.Id) })
            .Concat(_settings.Current.CustomTools.Select(x => new ToolCardViewModel(x, Loc, pinned.Contains(x.Id))))
            .ToList();

        var order = _settings.Current.ToolboxOrder.Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index, StringComparer.Ordinal);
        _allCards = _allCards.OrderByDescending(x => x.IsPinned)
            .ThenBy(x => order.TryGetValue(x.Id, out var index) ? index : int.MaxValue)
            .ToList();
        ApplyFilter();
    }

    private object? QuickToggleTargetFor(ToolDescriptor descriptor) =>
        descriptor.IsAvailable && QuickToggleToolIds.Contains(descriptor.Id) ? _pageFactory(descriptor) : null;

    public void AddCustomTool(CustomToolItem tool)
    {
        _settings.Current.CustomTools.Add(tool);
        _settings.Save();
        RefreshTools();
    }

    public void UpdateCustomTool(CustomToolItem tool)
    {
        var existing = _settings.Current.CustomTools.FirstOrDefault(x => x.Id == tool.Id);
        if (existing is null)
        {
            return;
        }

        existing.Name = tool.Name;
        existing.Target = tool.Target;
        existing.Kind = tool.Kind;
        existing.IconPngBase64 = tool.IconPngBase64;
        _settings.Save();
        RefreshTools();
    }

    public void DeleteCustomTool(string id)
    {
        _settings.Current.CustomTools.RemoveAll(x => x.Id == id);
        _settings.Current.ToolboxOrder.RemoveAll(x => x == id);
        _settings.Current.PinnedToolIds.RemoveAll(x => x == id);
        _settings.Save();
        RefreshTools();
    }

    public void TogglePin(ToolCardViewModel tool)
    {
        if (tool.IsPinned)
        {
            _settings.Current.PinnedToolIds.RemoveAll(x => x == tool.Id);
        }
        else if (!_settings.Current.PinnedToolIds.Contains(tool.Id, StringComparer.Ordinal))
        {
            _settings.Current.PinnedToolIds.Add(tool.Id);
        }

        _settings.Save();
        RefreshTools();
    }

    public void MoveTool(string sourceId, string targetId)
    {
        var sourceIndex = _allCards.FindIndex(x => x.Id == sourceId);
        var targetIndex = _allCards.FindIndex(x => x.Id == targetId);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
        {
            return;
        }

        var source = _allCards[sourceIndex];
        _allCards.RemoveAt(sourceIndex);
        _allCards.Insert(targetIndex, source);
        _settings.Current.ToolboxOrder = _allCards.Select(x => x.Id).ToList();
        _settings.Save();
        ApplyFilter();
    }

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
    private void OpenTool(ToolCardViewModel card)
    {
        if (card.Descriptor is { } descriptor)
        {
            _openTool(descriptor);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(card.Target) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(DragToSortTooltip));
        OnPropertyChanged(nameof(PinnedTooltip));
        // Card names/descriptions and the "pin"/"edit"/"delete" menu headers are read fresh each time
        // (ToolCardViewModel refreshes itself; menu headers are assigned when the menu opens), but the
        // search filter compares against card names, so re-run it in the new language.
        ApplyFilter();
    }
}
