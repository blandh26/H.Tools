using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using HTools.Core.Models;
using HTools.Core.Services;
using HTools.Windows.Imaging;

namespace HTools.App.Views;

public partial class CustomToolDialog : Window
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private readonly ILocalizationService _loc;
    private readonly CustomToolItem? _existing;
    private bool _loading;
    private bool _settingName;
    private bool _nameManuallyEdited;

    /// <summary>Design-time only (the XAML previewer needs a parameterless constructor).</summary>
    public CustomToolDialog()
        : this(new LocalizationService(), null)
    {
    }

    public CustomToolDialog(ILocalizationService loc, CustomToolItem? existing = null)
    {
        _loading = true;
        _loc = loc;
        InitializeComponent();
        _existing = existing;
        ApplyTexts();
        if (existing is not null)
        {
            NameBox.Text = existing.Name;
            KindBox.SelectedIndex = existing.Kind == "url" ? 1 : 0;
            TargetBox.Text = existing.Target;
        }

        _loading = false;
        UpdateKindControls();
    }

    public CustomToolItem? Result { get; private set; }

    /// <summary>The dialog is modal and short-lived, so its text is filled once when it opens rather
    /// than bound — the language can't change while it's on screen.</summary>
    private void ApplyTexts()
    {
        Title = _loc.Translate(_existing is null ? "CustomTool.AddTitle" : "CustomTool.EditTitle");
        NameLabel.Text = _loc.Translate("CustomTool.Name");
        NameBox.PlaceholderText = _loc.Translate("CustomTool.NamePlaceholder");
        KindLabel.Text = _loc.Translate("CustomTool.Kind");
        ExeKindItem.Content = _loc.Translate("CustomTool.KindExe");
        UrlKindItem.Content = _loc.Translate("CustomTool.KindUrl");
        TargetLabel.Text = _loc.Translate("CustomTool.Target");
        BrowseButton.Content = _loc.Translate("CustomTool.Browse");
        SaveButton.Content = _loc.Translate("Common.Save");
        CancelButton.Content = _loc.Translate("Common.Cancel");
    }

    private void OnKindChanged(object? sender, SelectionChangedEventArgs e) => UpdateKindControls();

    private void OnNameChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_loading && !_settingName)
        {
            _nameManuallyEdited = true;
        }
    }

    private void UpdateKindControls()
    {
        if (BrowseButton is null || KindBox is null || TargetBox is null)
        {
            return;
        }

        BrowseButton.IsVisible = KindBox.SelectedIndex == 0;
        TargetBox.PlaceholderText = _loc.Translate(KindBox.SelectedIndex == 0
            ? "CustomTool.TargetPlaceholderExe"
            : "CustomTool.TargetPlaceholderUrl");
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(_loc.Translate("CustomTool.ExeFileType")) { Patterns = ["*.exe"] }],
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path))
        {
            TargetBox.Text = path;
            if (!_nameManuallyEdited)
            {
                _settingName = true;
                NameBox.Text = Path.GetFileNameWithoutExtension(path);
                _settingName = false;
            }
            var icon = SvgRasterizer.ExtractExecutableIcon(path);
            _existingIconBytes = icon;
        }
    }

    private byte[]? _existingIconBytes;

    private async void OnTargetLostFocus(object? sender, RoutedEventArgs e)
    {
        if (KindBox.SelectedIndex == 0)
        {
            PopulateExecutableMetadata();
            return;
        }

        await PopulateUrlMetadataAsync();
    }

    private void PopulateExecutableMetadata()
    {
        var path = TargetBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)
            || !string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_nameManuallyEdited)
        {
            _settingName = true;
            NameBox.Text = Path.GetFileNameWithoutExtension(path);
            _settingName = false;
        }
        _existingIconBytes = SvgRasterizer.ExtractExecutableIcon(path);
    }

    private async Task PopulateUrlMetadataAsync()
    {
        if (KindBox.SelectedIndex != 1 || !Uri.TryCreate(TargetBox.Text?.Trim(), UriKind.Absolute, out var pageUri)
            || pageUri.Scheme is not ("http" or "https"))
        {
            return;
        }

        try
        {
            using var response = await Http.GetAsync(pageUri);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            if (!_nameManuallyEdited)
            {
                var titleMatch = Regex.Match(html, "<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var title = titleMatch.Success ? WebUtility.HtmlDecode(Regex.Replace(titleMatch.Groups[1].Value, "<[^>]+>", "")).Trim() : "";
                if (!string.IsNullOrWhiteSpace(title))
                {
                    _settingName = true;
                    NameBox.Text = title;
                    _settingName = false;
                }
            }

            var iconHref = "";
            foreach (Match link in Regex.Matches(html, "<link\\b[^>]*>", RegexOptions.IgnoreCase))
            {
                var rel = Regex.Match(link.Value, "\\brel\\s*=\\s*['\\\"]([^'\\\"]+)['\\\"]", RegexOptions.IgnoreCase);
                var href = Regex.Match(link.Value, "\\bhref\\s*=\\s*['\\\"]([^'\\\"]+)['\\\"]", RegexOptions.IgnoreCase);
                if (rel.Success && href.Success && rel.Groups[1].Value.Contains("icon", StringComparison.OrdinalIgnoreCase))
                {
                    iconHref = WebUtility.HtmlDecode(href.Groups[1].Value);
                    break;
                }
            }
            var iconUri = !string.IsNullOrWhiteSpace(iconHref) ? new Uri(pageUri, iconHref) : new Uri(pageUri, "/favicon.ico");
            using var iconResponse = await Http.GetAsync(iconUri);
            if (!iconResponse.IsSuccessStatusCode)
            {
                return;
            }

            var bytes = await iconResponse.Content.ReadAsByteArrayAsync();
            var png = bytes.Length > 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                ? bytes
                : SvgRasterizer.ConvertIconToPng(bytes);
            if (png is not null)
            {
                _existingIconBytes = png;
            }
        }
        catch
        {
            // Title and favicon lookup are best-effort; the URL remains usable when a site blocks requests.
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (KindBox.SelectedIndex == 1)
        {
            await PopulateUrlMetadataAsync();
        }
        else
        {
            PopulateExecutableMetadata();
        }
        var name = NameBox.Text?.Trim();
        var target = TargetBox.Text?.Trim();
        var kind = KindBox.SelectedIndex == 1 ? "url" : "exe";
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(target))
        {
            ValidationText.Text = _loc.Translate("CustomTool.ErrorRequired");
            return;
        }

        if (kind == "exe" && (!File.Exists(target) || !string.Equals(Path.GetExtension(target), ".exe", StringComparison.OrdinalIgnoreCase)))
        {
            ValidationText.Text = _loc.Translate("CustomTool.ErrorInvalidExe");
            return;
        }

        if (kind == "url" && (!Uri.TryCreate(target, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
        {
            ValidationText.Text = _loc.Translate("CustomTool.ErrorInvalidUrl");
            return;
        }

        Result = new CustomToolItem
        {
            Id = _existing?.Id ?? Guid.NewGuid().ToString("N"),
            Name = name,
            Target = target,
            Kind = kind,
            IconPngBase64 = kind == "exe"
                ? ToBase64(_existingIconBytes ?? SvgRasterizer.ExtractExecutableIcon(target))
                : ToBase64(_existingIconBytes),
        };
        Close(Result);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);

    private static string? ToBase64(byte[]? bytes) => bytes is null ? null : Convert.ToBase64String(bytes);
}
