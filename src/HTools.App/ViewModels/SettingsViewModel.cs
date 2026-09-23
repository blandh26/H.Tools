using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Services;
using WindowsStartup = HTools.Windows.Services.StartupManager;

namespace HTools.App.ViewModels;

public sealed partial class SettingsViewModel : LocalizedViewModelBase
{
    private const string AuthorEmail = "blandh26@gmail.com";
    private const string WebsiteUrl = "https://www.kimchicoder.com";

    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public SettingsViewModel(ILocalizationService loc, AppSettingsContext settings)
        : base(loc)
    {
        _settings = settings;
        // 每个语言项附带国旗图标，下拉框显示为"国旗 + 本地语言名"
        Languages = new ObservableCollection<LanguageItem>(
            loc.SupportedLanguages.Select(l => new LanguageItem(l.Code, l.NativeName, LanguageFlags.TryGet(l.Code))));

        _suppressPersist = true;
        SelectedLanguage = Languages.FirstOrDefault(l => l.Code == settings.Current.Language) ?? Languages[0];
        IsStartWithWindowsEnabled = WindowsStartup.IsEnabled();
        IsDarkThemeEnabled = settings.Current.IsDarkTheme;
        MinToolCardWidth = Math.Clamp(settings.Current.MinToolCardWidth, 160, 360);
        _suppressPersist = false;
    }

    public ObservableCollection<LanguageItem> Languages { get; }

    public string Title => Loc.Translate("Settings.Title");

    public string LanguageLabel => Loc.Translate("Settings.Language");

    public string StartWithWindowsLabel => Loc.Translate("Settings.StartWithWindows");

    public string StartWithWindowsHint => Loc.Translate("Settings.StartWithWindowsHint");

    public string DarkThemeLabel => Loc.Translate("Settings.DarkTheme");

    public string MinToolCardWidthLabel => Loc.Translate("Settings.MinToolCardWidth");

    public string MinToolCardWidthHint => Loc.Translate("Settings.MinToolCardWidthHint");

    public string AboutLabel => Loc.Translate("Settings.About");

    public string AuthorLabel => Loc.Translate("Settings.Author");

    public string WebsiteLabel => Loc.Translate("Settings.Website");

    public string AuthorEmailText => AuthorEmail;

    public string WebsiteText => WebsiteUrl;

    public AppSettingsContext SettingsContext => _settings;

    [RelayCommand]
    private static void OpenEmail() => OpenUrl($"mailto:{AuthorEmail}");

    [RelayCommand]
    private static void OpenWebsite() => OpenUrl(WebsiteUrl);

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception)
        {
            // Best-effort; no default handler registered for this URL scheme.
        }
    }

    [ObservableProperty]
    private LanguageItem? _selectedLanguage;

    [ObservableProperty]
    private bool _isStartWithWindowsEnabled;

    [ObservableProperty]
    private bool _isDarkThemeEnabled;

    [ObservableProperty]
    private int _minToolCardWidth;

    partial void OnSelectedLanguageChanged(LanguageItem? value)
    {
        if (_suppressPersist || value is null)
        {
            return;
        }

        Loc.SetLanguage(value.Code);
        _settings.Current.Language = value.Code;
        _settings.Save();
    }

    partial void OnIsStartWithWindowsEnabledChanged(bool value)
    {
        if (_suppressPersist)
        {
            return;
        }

        WindowsStartup.SetEnabled(value);
        _settings.Current.StartWithWindows = value;
        _settings.Save();
    }

    partial void OnIsDarkThemeEnabledChanged(bool value)
    {
        ApplyTheme(value);

        if (_suppressPersist)
        {
            return;
        }

        _settings.Current.IsDarkTheme = value;
        _settings.Save();
    }

    partial void OnMinToolCardWidthChanged(int value)
    {
        var boundedValue = Math.Clamp(value, 160, 360);
        if (boundedValue != value)
        {
            MinToolCardWidth = boundedValue;
            return;
        }

        if (_suppressPersist)
        {
            return;
        }

        _settings.Current.MinToolCardWidth = value;
        _settings.Save();
    }

    /// <summary>Applies the theme immediately (not just on save) so switching the toggle previews the
    /// change right away, matching every other setting on this page.</summary>
    public static void ApplyTheme(bool isDark) =>
        Application.Current!.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(StartWithWindowsLabel));
        OnPropertyChanged(nameof(StartWithWindowsHint));
        OnPropertyChanged(nameof(DarkThemeLabel));
        OnPropertyChanged(nameof(MinToolCardWidthLabel));
        OnPropertyChanged(nameof(MinToolCardWidthHint));
        OnPropertyChanged(nameof(AboutLabel));
        OnPropertyChanged(nameof(AuthorLabel));
        OnPropertyChanged(nameof(WebsiteLabel));
    }
}

/// <summary>语言下拉框的一项：语言代码、本地语言名（始终用该语言自身书写，如 "日本語"，
/// 不随界面语言翻译，方便看不懂当前界面语言的用户也能找到自己的语言）以及国旗图标。</summary>
public sealed record LanguageItem(string Code, string NativeName, Avalonia.Media.IImage? Flag);
