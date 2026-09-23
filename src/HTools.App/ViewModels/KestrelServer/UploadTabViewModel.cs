using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Services;
using HTools.Server.Modules;

namespace HTools.App.ViewModels.KestrelServer;

/// <summary>Standalone "file upload server" tool — see <see cref="StaticFileTabViewModel"/> for why
/// this used to be a tab and now isn't.</summary>
public sealed partial class UploadTabViewModel : LocalizedViewModelBase, IAsyncDisposable
{
    private readonly FileUploadServerModule _module = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public UploadTabViewModel(ILocalizationService loc, AppSettingsContext settings)
        : base(loc)
    {
        _settings = settings;
        _module.FileReceived += (_, path) => Dispatcher.UIThread.Post(() => ReceivedFiles.Insert(0, path));

        var saved = settings.Current.KestrelServer.Upload;
        _suppressPersist = true;
        Port = saved.Port;
        UploadFolder = string.IsNullOrEmpty(saved.UploadFolder)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            : saved.UploadFolder;
        _suppressPersist = false;
    }

    public ObservableCollection<string> ReceivedFiles { get; } = [];

    public string Title => Loc.Translate("Tool.UploadServer.Name");

    public string PortLabel => Loc.Translate("MockServer.Port");

    public string StartLabel => Loc.Translate("MockServer.Start");

    public string StopLabel => Loc.Translate("MockServer.Stop");

    public string UploadFolderLabel => Loc.Translate("KestrelServer.UploadFolder");

    public string ReceivedFilesLabel => Loc.Translate("KestrelServer.ReceivedFiles");

    // ── "服务配置 / 访问与使用" 两个子页签上的静态文字 ──

    public string ConfigTabLabel => Loc.Translate("KestrelServer.UploadConfigTab");

    public string UsageTabLabel => Loc.Translate("KestrelServer.UploadUsageTab");

    public string ChooseFolderLabel => Loc.Translate("KestrelServer.ChooseFolder");

    public string AccessUrlLabel => Loc.Translate("KestrelServer.AccessUrl");

    public string HowToLabel => Loc.Translate("KestrelServer.UploadHowTo");

    public string HowToText => Loc.Translate("KestrelServer.UploadHowToText");

    public string CommandLineLabel => Loc.Translate("KestrelServer.UploadCommandLine");

    /// <summary>curl 用法 + 远程访问时替换 localhost / 放行防火墙的提示。</summary>
    public string RemoteHintText => Loc.Translate("KestrelServer.UploadRemoteHint");

    public string AccessUrl => $"http://localhost:{Port}/";

    public string UploadExample => $"curl -F \"file=@C:\\path\\to\\file.zip\" http://localhost:{Port}/";

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private string _uploadFolder = string.Empty;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private async Task ToggleAsync()
    {
        if (IsRunning)
        {
            await _module.StopAsync();
            IsRunning = false;
            return;
        }

        _module.UploadFolder = UploadFolder;
        _module.PageTitle = Loc.Translate("KestrelServer.UploadPageTitle");
        _module.UploadButtonText = Loc.Translate("KestrelServer.UploadButton");
        _module.SuccessText = Loc.Translate("KestrelServer.UploadSuccess");
        IsRunning = await _module.StartAsync(Port);
        ErrorMessage = IsRunning ? null : _module.LastError;
    }

    partial void OnPortChanged(int value)
    {
        Persist();
        OnPropertyChanged(nameof(AccessUrl));
        OnPropertyChanged(nameof(UploadExample));
    }

    partial void OnUploadFolderChanged(string value) => Persist();

    private void Persist()
    {
        if (_suppressPersist)
        {
            return;
        }

        var s = _settings.Current.KestrelServer.Upload;
        s.Port = Port;
        s.UploadFolder = UploadFolder;
        _settings.Save();
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PortLabel));
        OnPropertyChanged(nameof(StartLabel));
        OnPropertyChanged(nameof(StopLabel));
        OnPropertyChanged(nameof(UploadFolderLabel));
        OnPropertyChanged(nameof(ReceivedFilesLabel));
        OnPropertyChanged(nameof(ConfigTabLabel));
        OnPropertyChanged(nameof(UsageTabLabel));
        OnPropertyChanged(nameof(ChooseFolderLabel));
        OnPropertyChanged(nameof(AccessUrlLabel));
        OnPropertyChanged(nameof(HowToLabel));
        OnPropertyChanged(nameof(HowToText));
        OnPropertyChanged(nameof(CommandLineLabel));
        OnPropertyChanged(nameof(RemoteHintText));
    }

    public async ValueTask DisposeAsync() => await _module.DisposeAsync();
}
