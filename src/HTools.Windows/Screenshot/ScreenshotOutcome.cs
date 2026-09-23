namespace HTools.Windows.Screenshot;

public enum ScreenshotOutcome
{
    Cancelled,
    CopiedToClipboard,
    SavedToFile,
}

public sealed record ScreenshotResult(ScreenshotOutcome Outcome, string? SavedFilePath = null);
