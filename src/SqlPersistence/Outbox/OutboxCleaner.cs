using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;

class OutboxCleaner : FeatureStartupTask
{
    public OutboxCleaner(Func<DateTime, CancellationToken, Task> cleanup, Action<string, Exception, CancellationToken> criticalError, TimeSpan timeToKeepDeduplicationData, TimeSpan frequencyToRunCleanup, IAsyncTimer timer)
    {
        this.cleanup = cleanup;
        this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
        this.frequencyToRunCleanup = frequencyToRunCleanup;
        this.timer = timer;
        this.criticalError = criticalError;
    }

    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
    {
        var cleanupFailures = 0;
        timer.Start(
            callback: async (utcTime, token) =>
            {
                var dateTime = utcTime - timeToKeepDeduplicationData;
                await cleanup(dateTime, token).ConfigureAwait(false);
                cleanupFailures = 0;
            },
            interval: frequencyToRunCleanup,
            errorCallback: exception =>
            {
                log.Error("Error cleaning outbox data", exception);
                cleanupFailures++;
                if (cleanupFailures >= 10)
                {
                    criticalError("Failed to clean expired Outbox records after 10 consecutive unsuccessful attempts. The most likely cause of this is connectivity issues with the database.", exception, cancellationToken);
                    cleanupFailures = 0;
                }
            },
            delayStrategy: Task.Delay);
        return Task.CompletedTask;
    }

    protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) =>
        timer.Stop(cancellationToken);

    IAsyncTimer timer;
    Action<string, Exception, CancellationToken> criticalError;
    Func<DateTime, CancellationToken, Task> cleanup;
    TimeSpan timeToKeepDeduplicationData;
    TimeSpan frequencyToRunCleanup;

    static ILog log = LogManager.GetLogger<OutboxCleaner>();
}