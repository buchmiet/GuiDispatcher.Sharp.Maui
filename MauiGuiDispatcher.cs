using GuiDispatcher.Sharp.Contracts;
using MauiDispatcher = Microsoft.Maui.Dispatching.Dispatcher;
using MauiIDispatcher = Microsoft.Maui.Dispatching.IDispatcher;

namespace GuiDispatcher.Sharp.Maui;

/// <summary>
/// <see cref="IGuiDispatcher"/> implementation backed by a .NET MAUI dispatcher.
/// </summary>
public class MauiGuiDispatcher : IGuiDispatcher
{
    private readonly MauiIDispatcher _dispatcher;

    /// <summary>Creates an adapter for the MAUI dispatcher associated with the current thread.</summary>
    public MauiGuiDispatcher()
        : this(ResolveCurrentDispatcher())
    {
    }

    /// <summary>Creates an adapter for the specified MAUI dispatcher.</summary>
    public MauiGuiDispatcher(MauiIDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
    }

    public bool CheckAccess() => !_dispatcher.IsDispatchRequired;

    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!_dispatcher.Dispatch(action))
            throw CreateDispatchRejectedException();
    }

    public void Invoke(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Invoke(() =>
        {
            action();
            return true;
        });
    }

    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!_dispatcher.IsDispatchRequired)
        {
            action();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_dispatcher.Dispatch(() =>
            {
                try
                {
                    action();
                    completion.TrySetResult();
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
            }))
        {
            completion.TrySetException(CreateDispatchRejectedException());
        }

        return completion.Task;
    }

    public Task InvokeAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!_dispatcher.IsDispatchRequired)
            return action();

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_dispatcher.Dispatch(() => CompleteAsync(action, completion)))
            completion.TrySetException(CreateDispatchRejectedException());

        return completion.Task;
    }

    public T Invoke<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (!_dispatcher.IsDispatchRequired)
            return func();

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_dispatcher.Dispatch(() =>
            {
                try
                {
                    completion.TrySetResult(func());
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
            }))
        {
            throw CreateDispatchRejectedException();
        }

        return completion.Task.GetAwaiter().GetResult();
    }

    public IGuiTimer CreateTimer(TimeSpan interval) => new MauiGuiTimer(_dispatcher, interval);

    public IDisposable RunOnce(Action action, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(action);

        var timer = new MauiGuiTimer(_dispatcher, interval);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            timer.Tick -= handler;
            timer.Stop();
            action();
        };

        timer.Tick += handler;
        timer.Start();
        return timer;
    }

    internal static MauiIDispatcher ResolveCurrentDispatcher()
    {
        return MauiDispatcher.GetForCurrentThread()
               ?? throw new InvalidOperationException(
                   "No .NET MAUI dispatcher is associated with the current thread. " +
                   "Construct MauiGuiDispatcher on the UI thread or pass a page/window dispatcher explicitly.");
    }

    private static async void CompleteAsync(Func<Task> action, TaskCompletionSource completion)
    {
        try
        {
            await action().ConfigureAwait(false);
            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }

    private static InvalidOperationException CreateDispatchRejectedException() =>
        new("The .NET MAUI dispatcher rejected the operation.");
}

