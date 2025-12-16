using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Threading;
using WinRT;

namespace StartDeck;

public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Required for WinUI 3 single-file/AOT scenarios to handle COM correctly.
        ComWrappersSupport.InitializeComWrappers();

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}
