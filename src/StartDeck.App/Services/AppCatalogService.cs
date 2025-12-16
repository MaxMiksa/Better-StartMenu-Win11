using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using StartDeck.Filtering;
using StartDeck.Models;
using StartDeck.Sources;

namespace StartDeck.Services;

public sealed class AppCatalogService : IDisposable
{
    private readonly IAppSource _source;
    private readonly IReadOnlyList<IAppFilter> _filters;
    private readonly FileSystemWatcher? _systemWatcher;
    private readonly FileSystemWatcher? _userWatcher;
    private bool _disposed;

    public DateTimeOffset LastScanTime { get; private set; }

    public event EventHandler? CatalogChanged;

    public AppCatalogService(IAppSource source, IEnumerable<IAppFilter> filters)
    {
        _source = source;
        _filters = filters.ToList();

        _systemWatcher = CreateWatcher(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu));
        _userWatcher = CreateWatcher(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));
    }

    public async Task<IReadOnlyList<AppEntry>> RefreshAsync(CancellationToken cancellationToken)
    {
        var raw = await _source.GetAppsAsync(cancellationToken).ConfigureAwait(false);
        var filtered = raw.Where(entry => entry.IsValid && _filters.All(f => f.ShouldKeep(entry))).ToList();
        var deduped = DeduplicateUserPreferred(filtered);
        var grouped = RemoveEmptyGroupsAndSort(deduped);
        LastScanTime = DateTimeOffset.UtcNow;
        return grouped;
    }

    public bool ShouldRescan() => LastScanTime == default || DateTimeOffset.UtcNow - LastScanTime > TimeSpan.FromHours(1);

    private static List<AppEntry> DeduplicateUserPreferred(List<AppEntry> entries)
    {
        // Key by relative path to ensure user-level overrides system-level.
        var map = new ConcurrentDictionary<string, AppEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries.OrderBy(e => e.IsUserScope ? 0 : 1))
        {
            map.AddOrUpdate(entry.RelativePath, entry, (_, existing) =>
            {
                // If existing is system and incoming is user, replace.
                if (!existing.IsUserScope && entry.IsUserScope)
                {
                    return entry;
                }
                return existing;
            });
        }

        return map.Values.ToList();
    }

    private static List<AppEntry> RemoveEmptyGroupsAndSort(List<AppEntry> entries)
    {
        var groups = entries.GroupBy(e => e.Group, StringComparer.CurrentCultureIgnoreCase)
            .Where(g => g.Any())
            .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase);

        var output = new List<AppEntry>();
        foreach (var group in groups)
        {
            output.AddRange(group.OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase));
        }

        return output;
    }

    private FileSystemWatcher? CreateWatcher(string rootStartMenu)
    {
        var programs = Path.Combine(rootStartMenu, "Programs");
        if (!Directory.Exists(programs))
        {
            return null;
        }

        var watcher = new FileSystemWatcher(programs)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
        };

        watcher.Created += OnWatcherChanged;
        watcher.Deleted += OnWatcherChanged;
        watcher.Renamed += OnWatcherChanged;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    private void OnWatcherChanged(object sender, FileSystemEventArgs e)
    {
        CatalogChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _systemWatcher?.Dispose();
        _userWatcher?.Dispose();
    }
}
