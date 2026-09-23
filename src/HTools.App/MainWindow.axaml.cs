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

    private void ToggleMaximizeRestore() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void UpdateMaximizeGlyph()
    {
        // 🗗 (two overlapping squares) reads as "restore" the same way it does in every other app that
        // uses it for this purpose; 🗖 (one square) reads as "maximize".
        MaximizeGlyph.Text = WindowState == WindowState.Maximized ? "🗗" : "🗖";
    }
}
