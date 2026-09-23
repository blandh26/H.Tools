using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using HTools.Core.Models;
using HTools.Windows.Imaging;

namespace HTools.App.Views;

public partial class CustomToolDialog : Window
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private readonly CustomToolItem? _existing;
    private bool _loading;
    private bool _settingName;
    private bool _nameManuallyEdited;

    public CustomToolDialog()
        : this(null)
    {
    }

    public CustomToolDialog(CustomToolItem? existing)
    {
        _loading = true;
        InitializeComponent();
        _existing = existing;
        if (existing is not null)
        {
            Title = "修改工具";
            NameBox.Text = existing.Name;
            KindBox.SelectedIndex = existing.Kind == "url" ? 1 : 0;
            TargetBox.Text = existing.Target;
        }

        _loading = false;
        UpdateKindControls();
    }

    public CustomToolItem? Result { get; private set; }

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
        TargetBox.PlaceholderText = KindBox.SelectedIndex == 0 ? "选择 .exe 文件" : "输入 https:// 地址";
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Windows 程序") { Patterns = ["*.exe"] }],
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
            ValidationText.Text = "请填写工具名称和路径/网址。";
            return;
        }

        if (kind == "exe" && (!File.Exists(target) || !string.Equals(Path.GetExtension(target), ".exe", StringComparison.OrdinalIgnoreCase)))
        {
            ValidationText.Text = "请选择有效的 .exe 文件。";
            return;
        }

        if (kind == "url" && (!Uri.TryCreate(target, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
        {
            ValidationText.Text = "请输入有效的 http 或 https 网址。";
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
