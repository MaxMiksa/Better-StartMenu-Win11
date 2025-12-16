using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StartDeck.Services;
using StartDeck.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Windows.Foundation.Metadata;

namespace StartDeck;

public sealed partial class MainWindow : Window
{
    private readonly AppCatalogService _catalogService;
    private readonly IconService _iconService;
    private readonly ObservableCollection<GroupViewModel> _groups = new();
    private CancellationTokenSource? _cts;

    public MainWindow(AppCatalogService catalogService, IconService iconService)
    {
        _catalogService = catalogService;
        _iconService = iconService;
        this.InitializeComponent();
        this.Width = 680;
        this.Height = 560;
        this.ExtendsContentIntoTitleBar = true;
        this.Loaded += OnLoaded;
        GroupRepeater.ItemsSource = _groups;
        _catalogService.CatalogChanged += OnCatalogChanged;
        TryApplyBackdrop();
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AppEntryViewModel vm)
        {
            LaunchEntry(vm, runAsAdmin: false);
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await LoadCatalogAsync();
    }

    private void OnShutdownClick(object sender, RoutedEventArgs e)
    {
        TryStartProcess("shutdown", "/s /t 0");
    }

    private void OnRestartClick(object sender, RoutedEventArgs e)
    {
        TryStartProcess("shutdown", "/r /t 0");
    }

    private void OnSleepClick(object sender, RoutedEventArgs e)
    {
        TryStartProcess("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
    }

    private void OnCatalogChanged(object? sender, EventArgs e)
    {
        _ = DispatcherQueue.TryEnqueue(async () => await LoadCatalogAsync());
    }

    private void OnRunAsAdminClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is AppEntryViewModel vm)
        {
            LaunchEntry(vm, runAsAdmin: true);
        }
    }

    private void OnOpenLocationClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is AppEntryViewModel vm)
        {
            OpenFileLocation(vm);
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        UserDisplayName.Text = Environment.UserName;
        UserName.Text = Environment.UserName;

        await LoadCatalogAsync();
    }

    private async Task LoadCatalogAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            var entries = await _catalogService.RefreshAsync(_cts.Token);
            BuildGroups(entries, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // ignore cancellation
        }
    }

    private void BuildGroups(IReadOnlyList<Models.AppEntry> entries, CancellationToken token)
    {
        _groups.Clear();
        var grouped = entries.GroupBy(e => e.Group, StringComparer.CurrentCultureIgnoreCase)
                             .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase);

        foreach (var group in grouped)
        {
            var gvm = new GroupViewModel { Name = group.Key };
            foreach (var entry in group)
            {
                token.ThrowIfCancellationRequested();
                var vm = new AppEntryViewModel
                {
                    Name = entry.Name,
                    FullPath = entry.FullPath,
                    Group = entry.Group,
                    IsUserScope = entry.IsUserScope
                };
                gvm.Items.Add(vm);
                _ = LoadIconAsync(vm, entry.FullPath, token);
            }

            if (gvm.Items.Count > 0)
            {
                _groups.Add(gvm);
            }
        }
    }

    private async Task LoadIconAsync(AppEntryViewModel vm, string path, CancellationToken token)
    {
        try
        {
            var icon = await _iconService.GetIconImageAsync(path, DispatcherQueue, token);
            vm.Icon = icon;
        }
        catch
        {
            // ignore icon failures, default already handled
        }
    }

    private void LaunchEntry(AppEntryViewModel vm, bool runAsAdmin)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = vm.FullPath,
                UseShellExecute = true
            };
            if (runAsAdmin)
            {
                psi.Verb = "runas";
            }
            Process.Start(psi);
            this.Hide();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // UAC cancelled, ignore.
        }
        catch
        {
            // swallow for now; could log
        }
    }

    private void OpenFileLocation(AppEntryViewModel vm)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{vm.FullPath}\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch
        {
            // ignore failures
        }
    }

    private static void TryStartProcess(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch
        {
            // ignore
        }
    }

    private void TryApplyBackdrop()
    {
        if (ApiInformation.IsTypePresent("Microsoft.UI.Composition.SystemBackdrops.MicaBackdrop"))
        {
            try
            {
                SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                return;
            }
            catch
            {
                // ignore and try acrylic
            }
        }

        if (ApiInformation.IsTypePresent("Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicBackdrop"))
        {
            try
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
                return;
            }
            catch
            {
                // fallback to default brush via XAML
            }
        }
    }
}
