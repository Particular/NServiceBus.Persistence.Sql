using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class OutboxCleanerTests
{
    [Test]
    public async Task It_uses_correct_cut_off_time()
    {
        var timer = new FakeTimer();
        var cutOffTime = DateTime.MinValue;
        var cleaner = new TestableCleaner((time, token) =>
        {
            cutOffTime = time;
            return Task.CompletedTask;
        }, (m, e, _) => { }, TimeSpan.FromDays(7), TimeSpan.Zero, timer);

        await cleaner.Start().ConfigureAwait(false);

        var now = new DateTime(2017, 3, 31, 0, 0, 0);
        await timer.Tick(now).ConfigureAwait(false);

        var expected = new DateTime(2017, 3, 24, 0, 0, 0);
        Assert.AreEqual(expected, cutOffTime);
    }

    [Test]
    public async Task If_triggers_critical_action_after_10_failures()
    {
        var criticalActionTriggered = false;
        var timer = new FakeTimer();
        var cleaner = new TestableCleaner((time, token) => Task.CompletedTask,
            (m, e, _) => criticalActionTriggered = true, TimeSpan.FromDays(7), TimeSpan.Zero, timer);

        await cleaner.Start().ConfigureAwait(false);

        for (var i = 0; i < 9; i++)
        {
            timer.OnError(new Exception("Simulated!"));
        }

        Assert.That(criticalActionTriggered, Is.False);

        //Trigger the 10th time
        timer.OnError(new Exception("Simulated!"));
        Assert.That(criticalActionTriggered, Is.True);
        criticalActionTriggered = false;

        //Trigger again -- the counter should be reset
        timer.OnError(new Exception("Simulated!"));
        Assert.That(criticalActionTriggered, Is.False);
    }

    [Test]
    public async Task It_resets_the_failure_counter_after_successful_attempt()
    {
        var criticalActionTriggered = false;
        var timer = new FakeTimer();
        var cleaner = new TestableCleaner((time, token) => Task.CompletedTask,
            (m, e, _) => criticalActionTriggered = true, TimeSpan.FromDays(7), TimeSpan.Zero, timer);

        await cleaner.Start().ConfigureAwait(false);

        for (var i = 0; i < 100; i++)
        {
            if (i % 9 == 0) //Succeed every 9th attempt
            {
                await timer.Tick(DateTime.UtcNow).ConfigureAwait(false);
            }
            else
            {
                timer.OnError(new Exception("Simulated!"));
            }
        }

        Assert.That(criticalActionTriggered, Is.False);
    }

    class TestableCleaner : OutboxCleaner
    {
        public TestableCleaner(Func<DateTime, CancellationToken, Task> cleanup, Action<string, Exception, CancellationToken> criticalError, TimeSpan timeToKeepDeduplicationData, TimeSpan frequencyToRunCleanup, IAsyncTimer timer)
            : base(cleanup, criticalError, timeToKeepDeduplicationData, frequencyToRunCleanup, timer)
        {
        }

        public Task Start(CancellationToken cancellationToken = default)
        {
            return OnStart(null, cancellationToken);
        }
    }

    class FakeTimer : IAsyncTimer
    {
        public Task Tick(DateTime utcTime, CancellationToken cancellationToken = default)
        {
            return callback(utcTime, cancellationToken);
        }

        public void OnError(Exception error) =>
            errorCallback(error);

        public void Start(Func<DateTime, CancellationToken, Task> callback, TimeSpan interval, Action<Exception> errorCallback, Func<TimeSpan, CancellationToken, Task> delayStrategy)
        {
            this.callback = callback;
            this.errorCallback = errorCallback;
        }

        public Task Stop(CancellationToken cancellationToken = default) => Task.CompletedTask;

        Func<DateTime, CancellationToken, Task> callback;
        Action<Exception> errorCallback;
    }
}