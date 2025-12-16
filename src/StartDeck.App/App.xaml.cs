using System;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using StartDeck.Services;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Graphics;
using StartDeck.Filtering;
using StartDeck.Sources;

namespace StartDeck;

public partial class App : Application
{
    private Window? _window;
    private AppInstance? _appInstance;
    private string? _pendingArguments;
    private bool _isWindowVisible;
    private readonly bool _suppressHideForDebug;
    private readonly IconService _iconService = new();
    private readonly AppCatalogService _catalogService;
    private readonly PositioningService _positioningService = new();
    private readonly Stopwatch _launchStopwatch = Stopwatch.StartNew();
    private bool _firstShowLogged;

    public App()
    {
        InitializeComponent();
        _catalogService = new AppCatalogService(
            new FileSystemStartMenuSource(),
            new IAppFilter[]
            {
                new ExtensionFilter(".lnk"),
                new KeywordFilter()
            });
        _suppressHideForDebug = Debugger.IsAttached ||
                                string.Equals(Environment.GetEnvironmentVariable("STARTDECK_DEBUG_NOHIDE"), "1", StringComparison.OrdinalIgnoreCase);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (!EnsureSingleInstance(args.Arguments))
        {
            Environment.Exit(0);
            return;
        }

        EnsureWindow();
        ToggleWindow(); // First launch: show once preheated window after creation.
        HandleActivationPayload(_pendingArguments);
    }

    private bool EnsureSingleInstance(string? args)
    {
        const string key = "main";
        var mainInstance = AppInstance.FindOrRegisterForKey(key);

        if (!mainInstance.IsCurrent)
        {
            AppInstance.GetCurrent().RedirectActivationToAsync(mainInstance).AsTask().Wait();
            return false;
        }

        _appInstance = mainInstance;
        _appInstance.Activated += OnActivated;
        _pendingArguments = args;
        return true;
    }

    private void OnActivated(object? sender, AppActivationArguments activationArgs)
    {
        var payload = activationArgs.Data?.ToString();
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            EnsureWindow();
            if (activationArgs.Kind == ExtendedActivationKind.Launch)
            {
                ToggleWindow();
            }
            else
            {
                ShowWindow();
            }

            HandleActivationPayload(payload);
        });
    }

    private void EnsureWindow()
    {
        if (_window == null)
        {
            _window = new MainWindow(_catalogService, _iconService);
            _window.Activated += OnWindowActivated;
            _window.Closed += OnWindowClosed;
            _window.Hide(); // preheat hidden
            _isWindowVisible = false;
        }
    }

    private void ShowWindow()
    {
        if (_window == null)
        {
            return;
        }

        PositionWindow();
        _window.Activate();
        _isWindowVisible = true;
        LogFirstShow();
    }

    private void HideWindow()
    {
        if (_suppressHideForDebug || _window == null)
        {
            return;
        }

        _window.Hide();
        _isWindowVisible = false;
        _iconService.ScheduleTrimAfterHide(TimeSpan.FromMinutes(5));
        LogMemory("HideWindow");
    }

    private void ToggleWindow()
    {
        if (_isWindowVisible)
        {
            HideWindow();
        }
        else
        {
            ShowWindow();
        }
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated && _isWindowVisible)
        {
            HideWindow();
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        // Prevent full teardown; keep services alive for fast wake. If you want to exit, remove this handler.
        args.Handled = true;
        HideWindow();
    }

    private void PositionWindow()
    {
        var appWindow = GetAppWindow();
        if (appWindow == null)
        {
            return;
        }

        var size = appWindow.Size;
        if (size.Width <= 0 || size.Height <= 0)
        {
            size = new SizeInt32(680, 560);
        }

        var rect = _positioningService.GetPlacement(size.Width, size.Height);
        appWindow.MoveAndResize(rect);
    }

    private AppWindow? GetAppWindow()
    {
        if (_window == null)
        {
            return null;
        }

        var hwnd = WindowNative.GetWindowHandle(_window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private static void HandleActivationPayload(string? payload)
    {
        // Placeholder for future activation data (e.g., --search).
        _ = payload;
    }

    private void LogFirstShow()
    {
        if (_firstShowLogged)
        {
            return;
        }

        _firstShowLogged = true;
        _launchStopwatch.Stop();
        Debug.WriteLine($"StartDeck: Activation->FirstShow {_launchStopwatch.ElapsedMilliseconds} ms");
        LogMemory("FirstShow");
    }

    private static void LogMemory(string context)
    {
        var bytes = GC.GetTotalMemory(false);
        Debug.WriteLine($"StartDeck: {context} GC total {bytes / 1024 / 1024} MB");
    }
}
