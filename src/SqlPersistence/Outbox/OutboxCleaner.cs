using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;

class OutboxCleaner : FeatureStartupTask
{
    public OutboxCleaner(Func<DateTime, CancellationToken, Task> cleanup, Action<string, Exception> criticalError, TimeSpan timeToKeepDeduplicationData, TimeSpan frequencyToRunCleanup, IAsyncTimer timer)
    {
        this.cleanup = cleanup;
        this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
        this.frequencyToRunCleanup = frequencyToRunCleanup;
        this.timer = timer;
        this.criticalError = criticalError;
    }

    protected override Task OnStart(IMessageSession session)
    {
        var cleanupFailures = 0;
        timer.Start(async (utcTime, token) =>
        {
            var dateTime = utcTime - timeToKeepDeduplicationData;
            await cleanup(dateTime, token);
            cleanupFailures = 0;
        }, frequencyToRunCleanup, exception =>
        {
            log.Error("Error cleaning outbox data", exception);
            cleanupFailures++;
            if (cleanupFailures >= 10)
            {
                criticalError("Failed to clean expired Outbox records after 10 consecutive unsuccessful attempts. The most likely cause of this is connectivity issues with the database.", exception);
                cleanupFailures = 0;
            }
        }, Task.Delay);
        return Task.FromResult(0);
    }

    protected override Task OnStop(IMessageSession session)
    {
        return timer.Stop();
    }

    IAsyncTimer timer;
    Action<string, Exception> criticalError;
    Func<DateTime, CancellationToken, Task> cleanup;
    TimeSpan timeToKeepDeduplicationData;
    TimeSpan frequencyToRunCleanup;

    static ILog log = LogManager.GetLogger<OutboxCleaner>();
}