using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StartDeck.Threading;

/// <summary>
/// Single-threaded STA task scheduler for COM/GDI work.
/// </summary>
public sealed class StaTaskScheduler : TaskScheduler, IDisposable
{
    private readonly BlockingCollection<Task> _tasks = new();
    private readonly Thread _thread;
    private readonly CancellationTokenSource _cts = new();

    public StaTaskScheduler()
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "STA-Worker"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    private void Run()
    {
        try
        {
            foreach (var task in _tasks.GetConsumingEnumerable(_cts.Token))
            {
                TryExecuteTask(task);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    protected override IEnumerable<Task>? GetScheduledTasks()
    {
        return _tasks.ToArray();
    }

    protected override void QueueTask(Task task)
    {
        _tasks.Add(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // Don't inline to maintain STA affinity.
        return false;
    }

    public override int MaximumConcurrencyLevel => 1;

    public void Dispose()
    {
        _cts.Cancel();
        _tasks.CompleteAdding();
    }
}
