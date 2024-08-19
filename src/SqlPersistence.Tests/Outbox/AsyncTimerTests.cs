using System;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class AsyncTimerTests
{
    [Test]
    public async Task It_calls_error_callback()
    {
        var errorCallbackInvoked = new TaskCompletionSource<bool>();

        var timer = new AsyncTimer();
        timer.Start((time, token) =>
        {
            throw new Exception("Simulated!");
        }, TimeSpan.Zero, e =>
        {
            errorCallbackInvoked.SetResult(true);
        }, Task.Delay);

        Assert.That(await errorCallbackInvoked.Task.ConfigureAwait(false), Is.True);
    }

    [Test]
    public async Task It_continues_to_run_after_an_error()
    {
        var callbackInvokedAfterError = new TaskCompletionSource<bool>();

        var fail = true;
        var exceptionThrown = false;
        var timer = new AsyncTimer();
        timer.Start((time, token) =>
        {
            if (fail)
            {
                fail = false;
                throw new Exception("Simulated!");
            }
            Assert.That(exceptionThrown, Is.True);
            callbackInvokedAfterError.SetResult(true);
            return Task.CompletedTask;
        }, TimeSpan.Zero, e =>
        {
            exceptionThrown = true;
        }, Task.Delay);

        Assert.That(await callbackInvokedAfterError.Task.ConfigureAwait(false), Is.True);
    }

    [Test]
    public async Task Stop_cancels_token_while_waiting()
    {
        var timer = new AsyncTimer();
        var waitCanceled = false;
        var delayStarted = new TaskCompletionSource<bool>();

        timer.Start((time, token) =>
        {
            throw new Exception("Simulated!");
        }, TimeSpan.FromDays(7), e =>
        {
            //noop
        }, async (delayTime, token) =>
        {
            delayStarted.SetResult(true);
            try
            {
                await Task.Delay(delayTime, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.IsCausedBy(token))
            {
                waitCanceled = true;
            }
        });
        await delayStarted.Task.ConfigureAwait(false);
        await timer.Stop().ConfigureAwait(false);

        Assert.That(waitCanceled, Is.True);
    }

    [Test]
    public async Task Stop_cancels_token_while_in_callback()
    {
        var timer = new AsyncTimer();
        var callbackCanceled = false;
        var callbackStarted = new TaskCompletionSource<bool>();
        var stopInitiated = new TaskCompletionSource<bool>();

        timer.Start(async (time, token) =>
        {
            callbackStarted.SetResult(true);
            await stopInitiated.Task.ConfigureAwait(false);
            if (token.IsCancellationRequested)
            {
                callbackCanceled = true;
            }
        }, TimeSpan.Zero, e =>
        {
            //noop
        }, Task.Delay);

        await callbackStarted.Task.ConfigureAwait(false);
        var stopTask = timer.Stop();
        stopInitiated.SetResult(true);
        await stopTask.ConfigureAwait(false);
        Assert.That(callbackCanceled, Is.True);
    }

    [Test]
    public async Task Stop_waits_for_callback_to_complete()
    {
        var timer = new AsyncTimer();

        var callbackCompleted = new TaskCompletionSource<bool>();
        var callbackTaskStarted = new TaskCompletionSource<bool>();

        timer.Start((time, token) =>
        {
            callbackTaskStarted.SetResult(true);
            return callbackCompleted.Task;
        }, TimeSpan.Zero, e =>
        {
            //noop
        }, Task.Delay);

        await callbackTaskStarted.Task.ConfigureAwait(false);

        var stopTask = timer.Stop();
        var delayTask = Task.Delay(1000);

        var firstToComplete = await Task.WhenAny(stopTask, delayTask).ConfigureAwait(false);
        Assert.AreEqual(delayTask, firstToComplete);
        callbackCompleted.SetResult(true);

        await stopTask.ConfigureAwait(false);
    }
}