using System;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using StartDeck.Services;

namespace StartDeck;

public partial class App : Application
{
    private Window? _window;
    private AppInstance? _appInstance;
    private string? _pendingArguments;
    private bool _isWindowVisible;
    private readonly bool _suppressHideForDebug;
    private readonly IconService _iconService = new();

    public App()
    {
        InitializeComponent();
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
            _window = new MainWindow();
            _window.Activated += OnWindowActivated;
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

        _window.Activate();
        _isWindowVisible = true;
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

    private static void HandleActivationPayload(string? payload)
    {
        // Placeholder for future activation data (e.g., --search).
        _ = payload;
    }
}
