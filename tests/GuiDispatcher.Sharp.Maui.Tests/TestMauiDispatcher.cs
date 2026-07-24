using System.Collections.Concurrent;
using Microsoft.Maui.Dispatching;

namespace GuiDispatcher.Sharp.Maui.Tests;

internal sealed class TestMauiDispatcher : IDispatcher, IDisposable
{
    private readonly BlockingCollection<Action> _queue = [];
    private readonly Thread _thread;
    private readonly TaskCompletionSource _ready =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _disposed;
    private int _threadId;

    public TestMauiDispatcher()
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "GuiDispatcher.Sharp.Maui test UI thread"
        };
        _thread.Start();
        _ready.Task.GetAwaiter().GetResult();
    }

    public bool IsDispatchRequired => Environment.CurrentManagedThreadId != _threadId;

    public bool Dispatch(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (Volatile.Read(ref _disposed) != 0)
            return false;

        try
        {
            _queue.Add(action);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public bool DispatchDelayed(TimeSpan delay, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (Volatile.Read(ref _disposed) != 0)
            return false;

        Timer? timer = null;
        timer = new Timer(_ =>
        {
            timer?.Dispose();
            Dispatch(action);
        }, null, delay, Timeout.InfiniteTimeSpan);
        return true;
    }

    public IDispatcherTimer CreateTimer() => new TestMauiDispatcherTimer(this);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _queue.CompleteAdding();
        _thread.Join(TimeSpan.FromSeconds(5));
        _queue.Dispose();
    }

    private void Run()
    {
        _threadId = Environment.CurrentManagedThreadId;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(this));
        _ready.TrySetResult();

        foreach (var action in _queue.GetConsumingEnumerable())
            action();
    }

    private sealed class DispatcherSynchronizationContext(TestMauiDispatcher dispatcher)
        : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state)
        {
            if (!dispatcher.Dispatch(() => callback(state)))
                throw new InvalidOperationException("The test dispatcher rejected a continuation.");
        }
    }
}

internal sealed class TestMauiDispatcherTimer(TestMauiDispatcher dispatcher) : IDispatcherTimer
{
    private Timer? _timer;
    private int _isRunning;

    public TimeSpan Interval { get; set; }

    public bool IsRepeating { get; set; }

    public bool IsRunning => Volatile.Read(ref _isRunning) != 0;

    public event EventHandler? Tick;

    public void Start()
    {
        if (Interlocked.Exchange(ref _isRunning, 1) != 0)
            return;

        _timer = new Timer(
            OnTimer,
            null,
            Interval,
            IsRepeating ? Interval : Timeout.InfiniteTimeSpan);
    }

    public void Stop()
    {
        if (Interlocked.Exchange(ref _isRunning, 0) == 0)
            return;

        _timer?.Dispose();
        _timer = null;
    }

    private void OnTimer(object? state)
    {
        if (!IsRunning)
            return;

        dispatcher.Dispatch(() =>
        {
            if (!IsRunning)
                return;

            Tick?.Invoke(this, EventArgs.Empty);

            if (!IsRepeating)
                Stop();
        });
    }
}

internal sealed class RejectingMauiDispatcher : IDispatcher
{
    public bool IsDispatchRequired => true;

    public bool Dispatch(Action action) => false;

    public bool DispatchDelayed(TimeSpan delay, Action action) => false;

    public IDispatcherTimer CreateTimer() =>
        throw new InvalidOperationException("The rejecting dispatcher cannot create timers.");
}

