namespace GuiDispatcher.Sharp.Maui.Tests;

public class MauiGuiDispatcherTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void Constructor_NullDispatcher_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MauiGuiDispatcher(null!));
    }

    [Fact]
    public async Task CheckAccess_OnMauiUiThread_ReturnsTrue()
    {
        using var nativeDispatcher = new TestMauiDispatcher();
        var dispatcher = new MauiGuiDispatcher(nativeDispatcher);
        var hasAccess = false;

        await dispatcher.InvokeAsync(() => hasAccess = dispatcher.CheckAccess());

        Assert.True(hasAccess);
    }

    [Fact]
    public async Task Post_FromBackgroundThread_RunsOnMauiUiThread()
    {
        using var nativeDispatcher = new TestMauiDispatcher();
        var dispatcher = new MauiGuiDispatcher(nativeDispatcher);
        var posted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        dispatcher.Post(() => posted.TrySetResult(dispatcher.CheckAccess()));

        Assert.True(await posted.Task.WaitAsync(Timeout));
    }

    [Fact]
    public void Invoke_FromBackgroundThread_RunsOnMauiUiThread_AndReturnsValue()
    {
        using var nativeDispatcher = new TestMauiDispatcher();
        var dispatcher = new MauiGuiDispatcher(nativeDispatcher);

        var result = dispatcher.Invoke(() => (dispatcher.CheckAccess(), Value: 42));

        Assert.True(result.Item1);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task InvokeAsync_FuncTask_DoesNotBlockUiThread_AndAwaitsInnerTask()
    {
        using var nativeDispatcher = new TestMauiDispatcher();
        var dispatcher = new MauiGuiDispatcher(nativeDispatcher);
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var postExecuted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var invokeTask = dispatcher.InvokeAsync(async () =>
        {
            Assert.True(dispatcher.CheckAccess());
            callbackStarted.SetResult();

            await callbackGate.Task.ConfigureAwait(true);

            Assert.True(dispatcher.CheckAccess());
        });

        await callbackStarted.Task.WaitAsync(Timeout);

        dispatcher.Post(() => postExecuted.SetResult());
        await postExecuted.Task.WaitAsync(Timeout);

        Assert.False(invokeTask.IsCompleted);

        callbackGate.SetResult();
        await invokeTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task RejectedDispatch_FailsInsteadOfHanging()
    {
        var dispatcher = new MauiGuiDispatcher(new RejectingMauiDispatcher());

        Assert.Throws<InvalidOperationException>(() => dispatcher.Post(() => { }));
        Assert.Throws<InvalidOperationException>(() => dispatcher.Invoke(() => { }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.InvokeAsync(() => { }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.InvokeAsync(async () =>
        {
            await Task.Yield();
        }));
    }

    [Fact]
    public async Task RunOnce_ExecutesOnceOnMauiUiThread()
    {
        using var nativeDispatcher = new TestMauiDispatcher();
        var dispatcher = new MauiGuiDispatcher(nativeDispatcher);
        var executed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var executeCount = 0;

        using var registration = dispatcher.RunOnce(() =>
        {
            Interlocked.Increment(ref executeCount);
            executed.TrySetResult(dispatcher.CheckAccess());
        }, TimeSpan.FromMilliseconds(50));

        Assert.True(await executed.Task.WaitAsync(Timeout));
        await Task.Delay(100);
        Assert.Equal(1, executeCount);
    }

    [Fact]
    public async Task CreateTimer_FiresOnMauiUiThread()
    {
        using var nativeDispatcher = new TestMauiDispatcher();
        var dispatcher = new MauiGuiDispatcher(nativeDispatcher);
        var ticked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var timer = dispatcher.CreateTimer(TimeSpan.FromMilliseconds(50));
        timer.Tick += (_, _) => ticked.TrySetResult(dispatcher.CheckAccess());
        timer.Start();

        Assert.True(await ticked.Task.WaitAsync(Timeout));
    }

    [Fact]
    public void NegativeIntervals_AreRejected()
    {
        using var nativeDispatcher = new TestMauiDispatcher();
        var dispatcher = new MauiGuiDispatcher(nativeDispatcher);
        var interval = TimeSpan.FromMilliseconds(-1);

        Assert.Throws<ArgumentOutOfRangeException>(() => dispatcher.CreateTimer(interval));
        Assert.Throws<ArgumentOutOfRangeException>(() => dispatcher.RunOnce(() => { }, interval));
    }
}

