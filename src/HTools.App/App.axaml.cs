using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HTools.App.Models;
using HTools.App.Services;
using HTools.App.ViewModels;
using HTools.Core.Services;
using HTools.Windows.Interop;
using HTools.Windows.Services;

namespace HTools.App;

public partial class App : Application
{
    private NativeMessageLoop? _nativeMessageLoop;
    private GlobalHotkeyManager? _hotkeyManager;
    private TrayIconService? _trayIcon;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsContext = new AppSettingsContext(new AppSettingsStore());
            var loc = new LocalizationService(settingsContext.Current.Language);
            SettingsViewModel.ApplyTheme(settingsContext.Current.IsDarkTheme);

            _nativeMessageLoop = new NativeMessageLoop();
            _nativeMessageLoop.Start();

            _hotkeyManager = new GlobalHotkeyManager(_nativeMessageLoop);

            var clipboardHistory = new ClipboardHistoryService();
            var dialogService = new DialogService(loc);

            var clipboardViewModel = new ClipboardViewModel(
                loc,
                clipboardHistory,
                _hotkeyManager,
                settingsContext,
                dialogService);

            var settingsViewModel = new SettingsViewModel(loc, settingsContext);

            var mouseEffectService = new MouseEffectService(_nativeMessageLoop);
            var mouseEffectViewModel = new MouseEffectViewModel(loc, mouseEffectService, settingsContext);

            var mockServerViewModel = new MockServerViewModel(loc, settingsContext);
            var kestrelServerViewModel = new KestrelServerViewModel(loc, settingsContext);

            var screenshotService = new ScreenshotService(_nativeMessageLoop);
            var screenshotViewModel = new ScreenshotViewModel(loc, screenshotService, _hotkeyManager, settingsContext, dialogService);

            object PageFactory(ToolDescriptor descriptor) => descriptor.Id switch
            {
                "clipboard" => clipboardViewModel,
                "mouse-effect" => mouseEffectViewModel,
                "mock-server" => mockServerViewModel,
                "kestrel-server" => kestrelServerViewModel,
                "screenshot" => screenshotViewModel,
                _ => new ComingSoonViewModel(descriptor, loc),
            };

            var mainWindowViewModel = new MainWindowViewModel(loc, settingsViewModel, PageFactory);
            var mainWindow = new MainWindow { DataContext = mainWindowViewModel };
            dialogService.Owner = mainWindow;

            _trayIcon = new TrayIconService(_nativeMessageLoop);
            _trayIcon.Initialize(loc.Translate("App.Title"), loc.Translate("Tray.Show"), loc.Translate("Tray.Exit"));
            loc.LanguageChanged += (_, _) =>
                _trayIcon.UpdateLabels(loc.Translate("Tray.Show"), loc.Translate("Tray.Exit"));

            mainWindow.PropertyChanged += (_, e) =>
            {
                if (e.Property == Window.WindowStateProperty && mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.Hide();
                }
            };

            _trayIcon.ShowRequested += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            });
            _trayIcon.ExitRequested += (_, _) => Dispatcher.UIThread.Post(() => desktop.Shutdown());

            desktop.MainWindow = mainWindow;
            desktop.ShutdownRequested += (_, _) =>
            {
                _trayIcon.Dispose();
                mouseEffectService.Dispose();
                mockServerViewModel.DisposeAsync().AsTask().GetAwaiter().GetResult();
                kestrelServerViewModel.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _hotkeyManager.Dispose();
                _nativeMessageLoop.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
