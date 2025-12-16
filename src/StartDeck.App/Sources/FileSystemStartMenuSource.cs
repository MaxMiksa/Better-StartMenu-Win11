using System;
using System.IO;
using System.Runtime.InteropServices;
using StartDeck.Models;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace StartDeck.Sources;

public sealed class FileSystemStartMenuSource : IAppSource
{
    private const string DefaultGroup = "Uncategorized";
    private readonly string _systemRoot;
    private readonly string _userRoot;

    public FileSystemStartMenuSource()
    {
        _systemRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs");
        _userRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
    }

    public Task<IReadOnlyList<AppEntry>> GetAppsAsync(CancellationToken cancellationToken)
    {
        var results = new List<AppEntry>();
        ScanRoot(_userRoot, isUserScope: true, results, cancellationToken);
        ScanRoot(_systemRoot, isUserScope: false, results, cancellationToken);
        return Task.FromResult<IReadOnlyList<AppEntry>>(results);
    }

    private void ScanRoot(string root, bool isUserScope, List<AppEntry> sink, CancellationToken ct)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(root, path);
            var group = GetGroupName(relative);
            var name = Path.GetFileNameWithoutExtension(path);
            var target = TryResolveTarget(path);
            var valid = !string.IsNullOrWhiteSpace(target);

            sink.Add(new AppEntry
            {
                Name = name,
                FullPath = path,
                RelativePath = relative,
                TargetPath = target,
                Group = string.IsNullOrWhiteSpace(group) ? DefaultGroup : group,
                IsUserScope = isUserScope,
                IsValid = valid
            });
        }
    }

    private static string GetGroupName(string relativePath)
    {
        var segments = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length <= 1)
        {
            return DefaultGroup;
        }

        return segments[0];
    }

    private static string? TryResolveTarget(string lnkPath)
    {
        try
        {
            var hr = PInvoke.CoCreateInstance(PInvoke.CLSID_ShellLink, null, CLSCTX.CLSCTX_INPROC_SERVER,
                typeof(IShellLinkW).GUID, out var shellLinkObj);
            if (hr.Failed)
            {
                return null;
            }

            var shellLink = (IShellLinkW)shellLinkObj;
            ((IPersistFile)shellLink).Load(lnkPath, (uint)STGM.STGM_READ);

            Span<char> buffer = stackalloc char[1024];
            var hres = shellLink.GetPath(ref MemoryMarshal.GetReference(buffer), buffer.Length, null, 0);
            if (hres.Failed)
            {
                return null;
            }

            var end = buffer.IndexOf('\0');
            if (end < 0)
            {
                end = buffer.Length;
            }

            var target = new string(buffer[..end]);
            return string.IsNullOrWhiteSpace(target) ? null : target;
        }
        catch (COMException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
