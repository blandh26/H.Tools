namespace HTools.Windows.Screenshot;

/// <summary>All user-facing text the overlay needs, supplied by the caller (which owns the
/// localization service) so this Win32/GDI+ layer stays free of any language dependency itself.</summary>
public sealed record ScreenshotOverlayTexts(
    string Hint,
    string SaveDialogFilter,
    string ToolRectangle,
    string ToolRectangleFilled,
    string ToolEllipse,
    string ToolEllipseFilled,
    string ToolLine,
    string ToolArrow,
    string ToolFreehand,
    string ToolText,
    string ToolMosaic,
    string ActionUndo,
    string ActionPin,
    string ActionCopy,
    string ActionSave,
    string ActionCancel,
    string CustomColor)
{
    public static ScreenshotOverlayTexts Default { get; } = new(
        "Drag to select a region, or hover a window and click to select it. Esc to cancel",
        "PNG image|*.png",
        "Rectangle",
        "Filled rectangle",
        "Ellipse",
        "Filled ellipse",
        "Line",
        "Arrow",
        "Freehand",
        "Text",
        "Mosaic",
        "Undo",
        "Pin to screen",
        "Copy to clipboard",
        "Save to file",
        "Cancel",
        "Custom color");
}
