using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using StartDeck.Caching;
using StartDeck.Threading;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace StartDeck.Services;

public sealed class IconService : IDisposable
{
    private const int DefaultSize = 48;
    private const int SoftLimit = 500;

    private readonly LruCache<string, byte[]> _cache = new(SoftLimit);
    private readonly StaTaskScheduler _staScheduler = new();
    private readonly byte[] _defaultIcon;
    private CancellationTokenSource? _hideTrimCts;

    public IconService()
    {
        _defaultIcon = CreateDefaultIconBytes();
    }

    public async Task<byte[]> GetIconBytesAsync(string path, CancellationToken cancellationToken)
    {
        if (_cache.TryGet(path, out var cached))
        {
            return cached;
        }

        var data = await Task.Factory.StartNew(() => ExtractIconPng(path, DefaultSize), cancellationToken,
            TaskCreationOptions.None, _staScheduler).ConfigureAwait(false);

        if (data == null || data.Length == 0)
        {
            return _defaultIcon;
        }

        _cache.AddOrUpdate(path, data);
        return data;
    }

    public async Task<BitmapImage> GetIconImageAsync(string path, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
    {
        var bytes = await GetIconBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var tcs = new TaskCompletionSource<BitmapImage>();

        dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                var image = new BitmapImage();
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(bytes.AsBuffer());
                stream.Seek(0);
                await image.SetSourceAsync(stream);
                tcs.TrySetResult(image);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    public void Trim()
    {
        _cache.Clear();
    }

    public void ScheduleTrimAfterHide(TimeSpan delay)
    {
        _hideTrimCts?.Cancel();
        _hideTrimCts = new CancellationTokenSource();
        var token = _hideTrimCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, token).ConfigureAwait(false);
                Trim();
                GC.Collect();
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }, token);
    }

    private static byte[]? ExtractIconPng(string path, int size)
    {
        HICON hIcon = default;
        try
        {
            var info = new SHFILEINFOW();
            var result = PInvoke.SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf(info),
                SHGFI.SHGFI_ICON | SHGFI.SHGFI_LARGEICON);

            if (result == 0 || info.hIcon == default)
            {
                return null;
            }

            hIcon = info.hIcon;
            using var icon = Icon.FromHandle(info.hIcon);
            using var bmp = icon.ToBitmap();
            using var resized = new Bitmap(bmp, new Size(size, size));
            using var ms = new MemoryStream();
            resized.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
        finally
        {
            if (hIcon != default)
            {
                PInvoke.DestroyIcon(hIcon);
            }
        }
    }

    private static byte[] CreateDefaultIconBytes()
    {
        using var icon = SystemIcons.Application;
        using var bmp = icon.ToBitmap();
        using var resized = new Bitmap(bmp, new Size(DefaultSize, DefaultSize));
        using var ms = new MemoryStream();
        resized.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    public void Dispose()
    {
        _staScheduler.Dispose();
    }
}
