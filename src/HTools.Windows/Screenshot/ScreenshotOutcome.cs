namespace HTools.Windows.Screenshot;

public enum ScreenshotOutcome
{
    Cancelled,
    CopiedToClipboard,
    SavedToFile,
    PinnedToScreen,
}

public sealed record ScreenshotResult(ScreenshotOutcome Outcome, string? SavedFilePath = null);
