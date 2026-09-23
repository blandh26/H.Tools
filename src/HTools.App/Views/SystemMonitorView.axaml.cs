using Avalonia.Controls;
using Avalonia.VisualTree;
using HTools.App.ViewModels;

namespace HTools.App.Views;

public partial class SystemMonitorView : UserControl
{
    private bool _isAttached;

    public SystemMonitorView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => { _isAttached = true; StartIfReady(); };
        DetachedFromVisualTree += (_, _) => { _isAttached = false; (DataContext as SystemMonitorViewModel)?.StopMonitoring(); };
        DataContextChanged += (_, _) => StartIfReady();
    }

    private void StartIfReady()
    {
        if (_isAttached)
            (DataContext as SystemMonitorViewModel)?.StartMonitoring();
    }
}
