using GuiDispatcher.Sharp.Contracts;
using MauiIDispatcher = Microsoft.Maui.Dispatching.IDispatcher;
using MauiIDispatcherTimer = Microsoft.Maui.Dispatching.IDispatcherTimer;

namespace GuiDispatcher.Sharp.Maui;

/// <summary>GUI timer backed by .NET MAUI's <see cref="MauiIDispatcherTimer"/>.</summary>
public class MauiGuiTimer : IGuiTimer
{
    private readonly MauiIDispatcherTimer _timer;

    /// <summary>Creates a timer on the MAUI dispatcher associated with the current thread.</summary>
    public MauiGuiTimer(TimeSpan interval)
        : this(MauiGuiDispatcher.ResolveCurrentDispatcher(), interval)
    {
    }

    /// <summary>Creates a timer on the specified MAUI dispatcher.</summary>
    public MauiGuiTimer(MauiIDispatcher dispatcher, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (interval < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than or equal to zero.");

        _timer = dispatcher.CreateTimer();
        _timer.Interval = interval;
        _timer.IsRepeating = true;
        _timer.Tick += OnTick;
    }

    public event EventHandler? Tick;

    public TimeSpan Interval
    {
        get => _timer.Interval;
        set
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Interval must be greater than or equal to zero.");

            _timer.Interval = value;
        }
    }

    public bool IsEnabled
    {
        get => _timer.IsRunning;
        set
        {
            if (value)
                _timer.Start();
            else
                _timer.Stop();
        }
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void OnTick(object? sender, EventArgs e) => Tick?.Invoke(this, e);
}

