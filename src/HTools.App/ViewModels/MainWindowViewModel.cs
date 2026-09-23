using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Models;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed partial class MainWindowViewModel : LocalizedViewModelBase
{
    private readonly Func<ToolDescriptor, object> _pageFactory;

    [ObservableProperty]
    private object? _currentPage;

    /// <summary>Whether the main window should stay above every other window. Session-only by design
    /// (not persisted) — it's a "right now" toggle, not a lasting preference, matching how always-on-top
    /// works in most other apps that offer it.</summary>
    [ObservableProperty]
    private bool _isAlwaysOnTop;


    public MainWindowViewModel(
        ILocalizationService loc,
        SettingsViewModel settingsPage,
        Func<ToolDescriptor, object> pageFactory)
        : base(loc)
    {
        _pageFactory = pageFactory;
        Settings = settingsPage;

        Home = new HomeViewModel(loc, OpenTool, pageFactory, settingsPage);
    }

    public HomeViewModel Home { get; }

    public SettingsViewModel Settings { get; }

    public string AppTitle => Loc.Translate("App.Title");

    public string WindowHeader => $"{AppTitle}  {typeof(MainWindowViewModel).Assembly.GetName().Version?.ToString(4) ?? "1.0.0.1"}";

    public string PinTooltip => Loc.Translate("MainWindow.Pin");

    public string MinimizeTooltip => Loc.Translate("MainWindow.Minimize");

    public string MaximizeTooltip => Loc.Translate("MainWindow.Maximize");

    public string CloseTooltip => Loc.Translate("MainWindow.Close");

    public string SettingsTooltip => Loc.Translate("Settings.Title");

    public object CurrentContent => CurrentPage ?? Home;

    public bool ShowBackButton => CurrentPage is not null;

    [RelayCommand]
    private void OpenSettings() => CurrentPage = Settings;

    [RelayCommand]
    private void GoHome() => CurrentPage = null;

    [RelayCommand]
    private void OpenTool(ToolDescriptor descriptor) => CurrentPage = _pageFactory(descriptor);

    partial void OnCurrentPageChanged(object? value)
    {
        OnPropertyChanged(nameof(CurrentContent));
        OnPropertyChanged(nameof(ShowBackButton));
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(AppTitle));
        OnPropertyChanged(nameof(WindowHeader));
        OnPropertyChanged(nameof(PinTooltip));
        OnPropertyChanged(nameof(MinimizeTooltip));
        OnPropertyChanged(nameof(MaximizeTooltip));
        OnPropertyChanged(nameof(CloseTooltip));
        OnPropertyChanged(nameof(SettingsTooltip));
    }
}
