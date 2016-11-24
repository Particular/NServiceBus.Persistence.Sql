using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;

class OutboxCleaner : FeatureStartupTask
{
    OutboxPersister outboxPersister;

    public OutboxCleaner(OutboxPersister outboxPersister, TimeSpan timeToKeepDeduplicationData, TimeSpan frequencyToRunCleanup)
    {
        cancellationTokenSource = new CancellationTokenSource();
        this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
        this.frequencyToRunCleanup = frequencyToRunCleanup;
        this.outboxPersister = outboxPersister;
    }

    protected override async Task OnStart(IMessageSession busSession)
    {
        await Task.Delay(TimeSpan.FromMinutes(1));
        while (true)
        {
            await Task.Delay(frequencyToRunCleanup, cancellationTokenSource.Token);
            if (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
            var dateTime = DateTime.UtcNow - timeToKeepDeduplicationData;
            await outboxPersister.RemoveEntriesOlderThan(dateTime, cancellationTokenSource.Token);
        }
    }

    protected override Task OnStop(IMessageSession busSession)
    {
        cancellationTokenSource.Cancel();
        return Task.FromResult(true);
    }

    TimeSpan timeToKeepDeduplicationData;
    TimeSpan frequencyToRunCleanup;
    CancellationTokenSource cancellationTokenSource;
}