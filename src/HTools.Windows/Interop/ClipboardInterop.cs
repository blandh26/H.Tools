using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace HTools.Windows.Interop;

/// <summary>
/// Thin wrapper around WinForms' OLE clipboard with retries, since another process can
/// transiently hold the clipboard lock (OpenClipboard fails with a busy error in that case).
/// </summary>
public static class ClipboardInterop
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(40);

    public static string? GetText()
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                return Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch (ExternalException)
            {
                Thread.Sleep(RetryDelay);
            }
        }

        return null;
    }

    public static bool SetText(string text)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch (ExternalException)
            {
                Thread.Sleep(RetryDelay);
            }
        }

        return false;
    }

    public static bool ContainsImage()
    {
        try
        {
            return Clipboard.ContainsImage();
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    /// <summary>Saves the current clipboard image as a new PNG in <paramref name="folder"/> and
    /// returns its path, or null if the clipboard has no image.</summary>
    public static string? CaptureImageToFile(string folder)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                if (!Clipboard.ContainsImage())
                {
                    return null;
                }

                using var image = Clipboard.GetImage();
                if (image is null)
                {
                    return null;
                }

                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, $"clip-{Guid.NewGuid():N}.png");
                image.Save(path, ImageFormat.Png);
                return path;
            }
            catch (ExternalException)
            {
                Thread.Sleep(RetryDelay);
            }
        }

        return null;
    }

    public static bool SetImage(Image image)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                Clipboard.SetImage(image);
                return true;
            }
            catch (ExternalException)
            {
                Thread.Sleep(RetryDelay);
            }
        }

        return false;
    }

    public static bool SetImageFromFile(string path)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                using var image = Image.FromFile(path);
                Clipboard.SetImage(image);
                return true;
            }
            catch (ExternalException)
            {
                Thread.Sleep(RetryDelay);
            }
            catch (IOException)
            {
                return false;
            }
        }

        return false;
    }
}
