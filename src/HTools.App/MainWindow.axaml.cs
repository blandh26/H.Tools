using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace HTools.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateMaximizeGlyph();
        UpdateWindowFrame();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Keep the Maximize/Restore button's glyph in sync with WindowState, including when the state
        // changes some way other than clicking that button — double-clicking the title bar, dragging to
        // a screen edge, or the taskbar's own restore/maximize context menu entry.
        if (change.Property == WindowStateProperty)
        {
            UpdateMaximizeGlyph();
            UpdateWindowFrame();
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // Double-clicking empty title bar space is the standard OS convention for toggling maximize —
        // native chrome gives this for free, but a fully custom title bar has to implement it explicitly.
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        BeginMoveDrag(e);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeRestoreClick(object? sender, RoutedEventArgs e) => ToggleMaximizeRestore();

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    // WindowDecorations="None" drops the OS's own edge/corner resize handling along with the rest of
    // its chrome, so these replace it — each is wired to a thin transparent strip around the window's
    // edge in MainWindow.axaml. WindowState.Normal-only isn't checked here because BeginResizeDrag is a
    // no-op while maximized anyway (there's nothing to resize into until it's restored).
    private void OnResizeWest(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.West, e);

    private void OnResizeEast(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.East, e);

    private void OnResizeNorth(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North, e);

    private void OnResizeSouth(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South, e);

    private void OnResizeNorthWest(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthWest, e);

    private void OnResizeNorthEast(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthEast, e);

    private void OnResizeSouthWest(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthWest, e);

    private void OnResizeSouthEast(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthEast, e);

    private void ToggleMaximizeRestore() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void UpdateMaximizeGlyph()
    {
        // 🗗 (two overlapping squares) reads as "restore" the same way it does in every other app that
        // uses it for this purpose; 🗖 (one square) reads as "maximize".
        MaximizeGlyph.Text = WindowState == WindowState.Maximized ? "🗗" : "🗖";
    }

    private void UpdateWindowFrame()
    {
        var isMaximized = WindowState == WindowState.Maximized;
        WindowFrame.Margin = isMaximized ? new Thickness(0) : new Thickness(5);
        WindowFrame.CornerRadius = isMaximized ? new CornerRadius(0) : new CornerRadius(14);
    }
}
