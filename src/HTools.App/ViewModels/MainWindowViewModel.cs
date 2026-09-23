using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Models;
using HTools.App.Services;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed partial class MainWindowViewModel : LocalizedViewModelBase
{
    private const int MaxRecentTools = 5;

    private readonly AppSettingsContext _settings;
    private readonly Func<ToolDescriptor, object> _pageFactory;

    [ObservableProperty]
    private string _selectedGroupKey = NavGroupKeys.Home;

    [ObservableProperty]
    private object? _currentPage;

    public MainWindowViewModel(
        ILocalizationService loc,
        AppSettingsContext settings,
        SettingsViewModel settingsPage,
        Func<ToolDescriptor, object> pageFactory)
        : base(loc)
    {
        _settings = settings;
        _pageFactory = pageFactory;
        Settings = settingsPage;

        NavGroups =
        [
            new NavGroupViewModel(NavGroupKeys.Home, "🏠", "Nav.Home", loc),
            new NavGroupViewModel(NavGroupKeys.DevTools, "🛠", "Nav.DevTools", loc),
            new NavGroupViewModel(NavGroupKeys.NetworkTools, "🌐", "Nav.NetworkTools", loc),
            new NavGroupViewModel(NavGroupKeys.Utilities, "🧰", "Nav.Utilities", loc),
            new NavGroupViewModel(NavGroupKeys.Settings, "⚙", "Nav.Settings", loc),
        ];
        NavGroups[0].IsSelected = true;

        Home = new HomeViewModel(loc, OpenTool, pageFactory);
        Home.ShowGroup(SelectedGroupKey);
        RefreshRecentTools();
    }

    public ObservableCollection<NavGroupViewModel> NavGroups { get; }

    public HomeViewModel Home { get; }

    public SettingsViewModel Settings { get; }

    public string AppTitle => Loc.Translate("App.Title");

    public object CurrentContent => CurrentPage ?? (SelectedGroupKey == NavGroupKeys.Settings ? Settings : Home);

    public bool ShowBackButton => CurrentPage is not null;

    [RelayCommand]
    private void SelectGroup(NavGroupViewModel group)
    {
        foreach (var navGroup in NavGroups)
        {
            navGroup.IsSelected = navGroup.Key == group.Key;
        }

        SelectedGroupKey = group.Key;
        CurrentPage = null;

        if (group.Key != NavGroupKeys.Settings)
        {
            Home.ShowGroup(group.Key);
        }
    }

    [RelayCommand]
    private void GoHome() => CurrentPage = null;

    private void OpenTool(ToolDescriptor descriptor)
    {
        CurrentPage = _pageFactory(descriptor);
        RememberRecentTool(descriptor.Id);
    }

    private void RememberRecentTool(string id)
    {
        var recents = _settings.Current.RecentToolIds;
        recents.RemoveAll(existing => existing == id);
        recents.Insert(0, id);
        if (recents.Count > MaxRecentTools)
        {
            recents.RemoveRange(MaxRecentTools, recents.Count - MaxRecentTools);
        }

        _settings.Save();
        RefreshRecentTools();
    }

    private void RefreshRecentTools()
    {
        var descriptors = _settings.Current.RecentToolIds
            .Select(ToolCatalog.FindById)
            .Where(d => d is not null)
            .Select(d => d!);
        Home.SetRecentTools(descriptors);
    }

    partial void OnCurrentPageChanged(object? value)
    {
        OnPropertyChanged(nameof(CurrentContent));
        OnPropertyChanged(nameof(ShowBackButton));
    }

    partial void OnSelectedGroupKeyChanged(string value) => OnPropertyChanged(nameof(CurrentContent));

    protected override void OnLanguageChanged() => OnPropertyChanged(nameof(AppTitle));
}
