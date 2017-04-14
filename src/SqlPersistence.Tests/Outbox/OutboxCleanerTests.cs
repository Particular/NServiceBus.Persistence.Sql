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
            return Task.FromResult(0);
        }, (m, e) => { }, TimeSpan.FromDays(7), TimeSpan.Zero, timer);

        await cleaner.Start();

        var now = new DateTime(2017, 3, 31, 0, 0, 0);
        await timer.Tick(now, CancellationToken.None);

        var expected = new DateTime(2017, 3, 24, 0, 0, 0);
        Assert.AreEqual(expected, cutOffTime);
    }

    [Test]
    public async Task If_triggers_critical_action_after_10_failures()
    {
        var criticalActionTriggered = false;
        var timer = new FakeTimer();
        var cleaner = new TestableCleaner((time, token) => Task.FromResult(0),
            (m, e) => criticalActionTriggered = true, TimeSpan.FromDays(7), TimeSpan.Zero, timer);

        await cleaner.Start();

        for (var i = 0; i < 9; i++)
        {
            timer.OnError(new Exception("Simulated!"));
        }

        Assert.IsFalse(criticalActionTriggered);

        //Trigger the 10th time
        timer.OnError(new Exception("Simulated!"));
        Assert.IsTrue(criticalActionTriggered);
        criticalActionTriggered = false;

        //Trigger again -- the counter should be reset
        timer.OnError(new Exception("Simulated!"));
        Assert.IsFalse(criticalActionTriggered);
    }

    [Test]
    public async Task It_resets_the_failure_counter_after_successful_attempt()
    {
        var criticalActionTriggered = false;
        var timer = new FakeTimer();
        var cleaner = new TestableCleaner((time, token) => Task.FromResult(0),
            (m, e) => criticalActionTriggered = true, TimeSpan.FromDays(7), TimeSpan.Zero, timer);

        await cleaner.Start();

        for (var i = 0; i < 100; i++)
        {
            if (i%9 == 0) //Succeed every 9th attempt
            {
                await timer.Tick(DateTime.UtcNow, CancellationToken.None);
            }
            else
            {
                timer.OnError(new Exception("Simulated!"));
            }
        }

        Assert.IsFalse(criticalActionTriggered);
    }

    class TestableCleaner : OutboxCleaner
    {
        public TestableCleaner(Func<DateTime, CancellationToken, Task> cleanup, Action<string, Exception> criticalError, TimeSpan timeToKeepDeduplicationData, TimeSpan frequencyToRunCleanup, IAsyncTimer timer) 
            : base(cleanup, criticalError, timeToKeepDeduplicationData, frequencyToRunCleanup, timer)
        {
        }

        public Task Start()
        {
            return OnStart(null);
        }
    }

    class FakeTimer : IAsyncTimer
    {
        public Task Tick(DateTime utcTime, CancellationToken token)
        {
            return callback(utcTime, token);
        }

        public void OnError(Exception error)
        {
            errorCallback(error);
        }

        public void Start(Func<DateTime, CancellationToken, Task> callback, TimeSpan interval, Action<Exception> errorCallback, Func<TimeSpan, CancellationToken, Task> delayStrategy)
        {
            this.callback = callback;
            this.errorCallback = errorCallback;
        }

        public Task Stop()
        {
            return Task.FromResult(0);
        }

        Func<DateTime, CancellationToken, Task> callback;
        Action<Exception> errorCallback;
    }
}
