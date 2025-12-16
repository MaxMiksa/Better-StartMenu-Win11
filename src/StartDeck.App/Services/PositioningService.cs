using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Microsoft.UI.Windowing;

namespace StartDeck.Services;

public sealed class PositioningService
{
    private const int TaskbarFallbackHeight = 48;
    private const int Tolerance = 1;

    public RectInt32 GetPlacement(int windowWidth, int windowHeight)
    {
        var cursor = GetCursor();
        var displayArea = DisplayArea.GetFromPoint(cursor, DisplayAreaFallback.Primary);
        var displayRect = displayArea.DisplayRect;
        var workArea = displayArea.WorkArea;

        var edge = InferTaskbarEdge(displayRect, workArea);

        int x, y;
        var mainTaskbar = GetPrimaryTaskbarHeight();

        switch (edge)
        {
            case TaskbarEdge.Bottom:
                x = workArea.X + (workArea.Width - windowWidth) / 2;
                y = workArea.Y + workArea.Height - windowHeight - mainTaskbar;
                break;
            case TaskbarEdge.Top:
                x = workArea.X + (workArea.Width - windowWidth) / 2;
                y = workArea.Y;
                break;
            case TaskbarEdge.Left:
                x = workArea.X;
                y = workArea.Y + (workArea.Height - windowHeight) / 2;
                break;
            case TaskbarEdge.Right:
                x = workArea.X + workArea.Width - windowWidth;
                y = workArea.Y + (workArea.Height - windowHeight) / 2;
                break;
            default:
                x = workArea.X + (workArea.Width - windowWidth) / 2;
                y = workArea.Y + workArea.Height - TaskbarFallbackHeight - windowHeight;
                break;
        }

        // Clamp within work area.
        x = Math.Max(workArea.X, Math.Min(x, workArea.X + workArea.Width - windowWidth));
        y = Math.Max(workArea.Y, Math.Min(y, workArea.Y + workArea.Height - windowHeight));

        return new RectInt32(x, y, windowWidth, windowHeight);
    }

    private static TaskbarEdge InferTaskbarEdge(RectInt32 displayRect, RectInt32 workArea)
    {
        if (Math.Abs(workArea.Y - displayRect.Y) > Tolerance)
        {
            return TaskbarEdge.Top;
        }
        if (Math.Abs((displayRect.Y + displayRect.Height) - (workArea.Y + workArea.Height)) > Tolerance)
        {
            return TaskbarEdge.Bottom;
        }
        if (Math.Abs(workArea.X - displayRect.X) > Tolerance)
        {
            return TaskbarEdge.Left;
        }
        if (Math.Abs((displayRect.X + displayRect.Width) - (workArea.X + workArea.Width)) > Tolerance)
        {
            return TaskbarEdge.Right;
        }
        return TaskbarEdge.Unknown;
    }

    private static PointInt32 GetCursor()
    {
        PInvoke.GetCursorPos(out POINT pt);
        return new PointInt32(pt.x, pt.y);
    }

    private static int GetPrimaryTaskbarHeight()
    {
        var abd = new APPBARDATA
        {
            cbSize = (uint)Marshal.SizeOf<APPBARDATA>()
        };
        var result = PInvoke.SHAppBarMessage(SHELLAPPBARMESSAGE.ABM_GETTASKBARPOS, ref abd);
        if (result != 0)
        {
            return abd.rc.bottom - abd.rc.top;
        }

        return TaskbarFallbackHeight;
    }

    private enum TaskbarEdge
    {
        Unknown,
        Top,
        Bottom,
        Left,
        Right
    }
}
