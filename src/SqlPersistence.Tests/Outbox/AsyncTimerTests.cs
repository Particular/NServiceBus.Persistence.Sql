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

        Assert.IsTrue(await errorCallbackInvoked.Task);
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
            Assert.IsTrue(exceptionThrown);
            callbackInvokedAfterError.SetResult(true);
            return Task.FromResult(0);
        }, TimeSpan.Zero, e =>
        {
            exceptionThrown = true;
        }, Task.Delay);

        Assert.IsTrue(await callbackInvokedAfterError.Task);
    }

    [Test]
    public async Task Stop_cancels_token_while_waiting()
    {
        var timer = new AsyncTimer();
        var waitCancelled = false;
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
                await Task.Delay(delayTime, token);
            }
            catch (OperationCanceledException)
            {
                waitCancelled = true;
            }
        });
        await delayStarted.Task;
        await timer.Stop();

        Assert.IsTrue(waitCancelled);
    }

    [Test]
    public async Task Stop_cancels_token_while_in_callback()
    {
        var timer = new AsyncTimer();
        var callbackCancelled = false;
        var callbackStarted = new TaskCompletionSource<bool>();
        var stopInitiated = new TaskCompletionSource<bool>();

        timer.Start(async (time, token) =>
        {
            callbackStarted.SetResult(true);
            await stopInitiated.Task;
            if (token.IsCancellationRequested)
            {
                callbackCancelled = true;
            }
        }, TimeSpan.Zero, e =>
        {
            //noop
        }, Task.Delay);

        await callbackStarted.Task;
        var stopTask = timer.Stop();
        stopInitiated.SetResult(true);
        await stopTask;
        Assert.IsTrue(callbackCancelled);
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

        await callbackTaskStarted.Task;

        var stopTask = timer.Stop();
        var delayTask = Task.Delay(1000);

        var firstToComplete = await Task.WhenAny(stopTask, delayTask);
        Assert.AreEqual(delayTask, firstToComplete);
        callbackCompleted.SetResult(true);

        await stopTask;
    }
}