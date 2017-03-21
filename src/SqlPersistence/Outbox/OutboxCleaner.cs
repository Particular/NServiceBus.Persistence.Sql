using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;

class OutboxCleaner : FeatureStartupTask
{
    public OutboxCleaner(OutboxPersister outboxPersister, CriticalError criticalError, TimeSpan timeToKeepDeduplicationData, TimeSpan frequencyToRunCleanup)
    {
        this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
        this.frequencyToRunCleanup = frequencyToRunCleanup;
        this.outboxPersister = outboxPersister;
        this.criticalError = criticalError;
    }

    protected override Task OnStart(IMessageSession session)
    {
        tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        task = Task.Run(async () =>
        {
            var cleanupFailures = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var dateTime = DateTime.UtcNow - timeToKeepDeduplicationData;
                    await Task.Delay(frequencyToRunCleanup, token);
                    await outboxPersister.RemoveEntriesOlderThan(dateTime, token);
                    cleanupFailures = 0;
                }
                catch (OperationCanceledException)
                {
                    // noop
                }
                catch (Exception exception)
                {
                    log.Error("Error cleaning outbox data", exception);
                    cleanupFailures++;
                    if (cleanupFailures >= 10)
                    {
                        criticalError.Raise("Failed to clean expired Outbox records after 10 consecutive unsuccessful attempts. The most likely cause of this is connectivity issues with the database.", exception);
                        cleanupFailures = 0;
                    }
                }
            }
        }, CancellationToken.None);
        return Task.FromResult(0);
    }

    protected override Task OnStop(IMessageSession session)
    {
        if (tokenSource == null)
        {
            return Task.FromResult(0);
        }

        tokenSource.Cancel();
        tokenSource.Dispose();

        return task ?? Task.FromResult(0);
    }

    OutboxPersister outboxPersister;
    CriticalError criticalError;
    TimeSpan timeToKeepDeduplicationData;
    TimeSpan frequencyToRunCleanup;
    Task task;
    CancellationTokenSource tokenSource;

    static ILog log = LogManager.GetLogger<OutboxCleaner>();
}