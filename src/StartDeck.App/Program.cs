using System;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using WinRT;

namespace StartDeck;

public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Initialize Windows App SDK bootstrapper for unpackaged scenarios.
        try
        {
            Bootstrap.Initialize();
        }
        catch
        {
            // If bootstrap fails, proceed; AppInstance APIs may still work in packaged env.
        }

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
