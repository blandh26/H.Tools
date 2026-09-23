using CommunityToolkit.Mvvm.ComponentModel;

namespace HTools.App.ViewModels.KestrelServer;

public sealed partial class MockRuleEditorItem : ObservableObject
{
    [ObservableProperty]
    private string _method = "GET";

    [ObservableProperty]
    private string _path = "/";

    [ObservableProperty]
    private int _statusCode = 200;

    [ObservableProperty]
    private string _contentType = "application/json";

    [ObservableProperty]
    private string _body = "{}";
}
