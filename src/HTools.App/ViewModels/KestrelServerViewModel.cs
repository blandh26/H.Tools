using HTools.App.Services;
using HTools.App.ViewModels.KestrelServer;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed class KestrelServerViewModel : LocalizedViewModelBase, IAsyncDisposable
{
    public KestrelServerViewModel(ILocalizationService loc, AppSettingsContext settings)
        : base(loc)
    {
        StaticFileTab = new StaticFileTabViewModel(settings);
        MockApiTab = new MockApiTabViewModel(settings);
        WebhookTab = new WebhookTabViewModel(settings);
        UploadTab = new UploadTabViewModel(settings, loc);
        ProxyTab = new ProxyTabViewModel(settings);
        LatencyTab = new LatencyTabViewModel(settings);
    }

    public StaticFileTabViewModel StaticFileTab { get; }

    public MockApiTabViewModel MockApiTab { get; }

    public WebhookTabViewModel WebhookTab { get; }

    public UploadTabViewModel UploadTab { get; }

    public ProxyTabViewModel ProxyTab { get; }

    public LatencyTabViewModel LatencyTab { get; }

    public string Title => Loc.Translate("KestrelServer.Title");

    public string StaticFileTabLabel => Loc.Translate("KestrelServer.StaticFileTab");

    public string MockApiTabLabel => Loc.Translate("KestrelServer.MockApiTab");

    public string WebhookTabLabel => Loc.Translate("KestrelServer.WebhookTab");

    public string UploadTabLabel => Loc.Translate("KestrelServer.UploadTab");

    public string ProxyTabLabel => Loc.Translate("KestrelServer.ProxyTab");

    public string LatencyTabLabel => Loc.Translate("KestrelServer.LatencyTab");

    public string PortLabel => Loc.Translate("MockServer.Port");

    public string StartLabel => Loc.Translate("MockServer.Start");

    public string StopLabel => Loc.Translate("MockServer.Stop");

    public string RootFolderLabel => Loc.Translate("KestrelServer.RootFolder");

    public string RulesLabel => Loc.Translate("KestrelServer.Rules");

    public string AddRuleLabel => Loc.Translate("KestrelServer.AddRule");

    public string RemoveRuleLabel => Loc.Translate("KestrelServer.RemoveRule");

    public string UploadFolderLabel => Loc.Translate("KestrelServer.UploadFolder");

    public string ReceivedFilesLabel => Loc.Translate("KestrelServer.ReceivedFiles");

    public string TargetUrlLabel => Loc.Translate("KestrelServer.TargetUrl");

    public string DelayMsLabel => Loc.Translate("KestrelServer.DelayMs");

    public string RequestLogLabel => Loc.Translate("MockServer.RequestLog");

    public string ClearLogLabel => Loc.Translate("MockServer.ClearLog");

    public string ApplyLabel => Loc.Translate("Common.Save");

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(StaticFileTabLabel));
        OnPropertyChanged(nameof(MockApiTabLabel));
        OnPropertyChanged(nameof(WebhookTabLabel));
        OnPropertyChanged(nameof(UploadTabLabel));
        OnPropertyChanged(nameof(ProxyTabLabel));
        OnPropertyChanged(nameof(LatencyTabLabel));
        OnPropertyChanged(nameof(PortLabel));
        OnPropertyChanged(nameof(StartLabel));
        OnPropertyChanged(nameof(StopLabel));
        OnPropertyChanged(nameof(RootFolderLabel));
        OnPropertyChanged(nameof(RulesLabel));
        OnPropertyChanged(nameof(AddRuleLabel));
        OnPropertyChanged(nameof(RemoveRuleLabel));
        OnPropertyChanged(nameof(UploadFolderLabel));
        OnPropertyChanged(nameof(ReceivedFilesLabel));
        OnPropertyChanged(nameof(TargetUrlLabel));
        OnPropertyChanged(nameof(DelayMsLabel));
        OnPropertyChanged(nameof(RequestLogLabel));
        OnPropertyChanged(nameof(ClearLogLabel));
        OnPropertyChanged(nameof(ApplyLabel));
    }

    public async ValueTask DisposeAsync()
    {
        await StaticFileTab.DisposeAsync();
        await MockApiTab.DisposeAsync();
        await WebhookTab.DisposeAsync();
        await UploadTab.DisposeAsync();
        await ProxyTab.DisposeAsync();
        await LatencyTab.DisposeAsync();
    }
}
