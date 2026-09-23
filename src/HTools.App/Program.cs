using Avalonia;
using Avalonia.Win32;
using System;

namespace HTools.App;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Force software (CPU) rendering instead of GPU-accelerated (ANGLE/OpenGL/Direct3D).
            // Symptom this fixes: the window shows as solid black with nothing painted on it — the
            // classic "Avalonia under RDP / a remote or virtualized display" failure, where the GPU
            // rendering context Avalonia tries first doesn't actually work in that kind of session even
            // though it initializes without throwing. Software rendering is slower but always works.
            .With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Software] })
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
