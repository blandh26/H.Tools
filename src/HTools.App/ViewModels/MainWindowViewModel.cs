using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Models;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed partial class MainWindowViewModel : LocalizedViewModelBase
{
    private readonly Func<ToolDescriptor, object> _pageFactory;

    [ObservableProperty]
    private string _selectedGroupKey = NavGroupKeys.Home;

    [ObservableProperty]
    private object? _currentPage;

    /// <summary>Whether the main window should stay above every other window. Session-only by design
    /// (not persisted) — it's a "right now" toggle, not a lasting preference, matching how always-on-top
    /// works in most other apps that offer it.</summary>
    [ObservableProperty]
    private bool _isAlwaysOnTop;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    public double SidebarWidth => IsSidebarExpanded ? 220 : 68;

    public MainWindowViewModel(
        ILocalizationService loc,
        SettingsViewModel settingsPage,
        Func<ToolDescriptor, object> pageFactory)
        : base(loc)
    {
        _pageFactory = pageFactory;
        Settings = settingsPage;

        NavGroups =
        [
            new NavGroupViewModel(NavGroupKeys.Home, "🗂", "Nav.Home", loc),
            new NavGroupViewModel(NavGroupKeys.DevTools, "🛠", "Nav.DevTools", loc),
            new NavGroupViewModel(NavGroupKeys.Utilities, "🧰", "Nav.Utilities", loc),
            new NavGroupViewModel(NavGroupKeys.Settings, "⚙", "Nav.Settings", loc),
        ];
        NavGroups[0].IsSelected = true;

        Home = new HomeViewModel(loc, OpenTool, pageFactory);
        Home.ShowGroup(SelectedGroupKey);
    }

    public ObservableCollection<NavGroupViewModel> NavGroups { get; }

    public HomeViewModel Home { get; }

    public SettingsViewModel Settings { get; }

    public string AppTitle => Loc.Translate("App.Title");

    public string PinTooltip => Loc.Translate("MainWindow.Pin");

    public string MinimizeTooltip => Loc.Translate("MainWindow.Minimize");

    public string MaximizeTooltip => Loc.Translate("MainWindow.Maximize");

    public string CloseTooltip => Loc.Translate("MainWindow.Close");

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

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

    private void OpenTool(ToolDescriptor descriptor) => CurrentPage = _pageFactory(descriptor);

    partial void OnCurrentPageChanged(object? value)
    {
        OnPropertyChanged(nameof(CurrentContent));
        OnPropertyChanged(nameof(ShowBackButton));
    }

    partial void OnSelectedGroupKeyChanged(string value) => OnPropertyChanged(nameof(CurrentContent));

    partial void OnIsSidebarExpandedChanged(bool value) => OnPropertyChanged(nameof(SidebarWidth));

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(AppTitle));
        OnPropertyChanged(nameof(PinTooltip));
        OnPropertyChanged(nameof(MinimizeTooltip));
        OnPropertyChanged(nameof(MaximizeTooltip));
        OnPropertyChanged(nameof(CloseTooltip));
    }
}
