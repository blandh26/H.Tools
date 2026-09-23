namespace HTools.Windows.Screenshot;

/// <summary>All user-facing text the overlay needs, supplied by the caller (which owns the
/// localization service) so this Win32/GDI+ layer stays free of any language dependency itself.</summary>
public sealed record ScreenshotOverlayTexts(
    string Hint,
    string SaveDialogFilter,
    string ToolRectangle,
    string ToolEllipse,
    string ToolArrow,
    string ToolFreehand,
    string ToolText,
    string ToolMosaic,
    string ActionUndo,
    string ActionCopy,
    string ActionSave,
    string ActionCancel)
{
    public static ScreenshotOverlayTexts Default { get; } = new(
        "Drag to select a region, Esc to cancel",
        "PNG image|*.png",
        "Rectangle",
        "Ellipse",
        "Arrow",
        "Freehand",
        "Text",
        "Mosaic",
        "Undo",
        "Copy to clipboard",
        "Save to file",
        "Cancel");
}
