using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;
using NServiceBus.Unicast.Subscriptions;
using NUnit.Framework;

[TestFixture]
public class SubscriptionCacheFailureTests
{
    static readonly MessageType messageType = new("type1", new Version(0, 0, 0, 0));

    [Test]
    public void Failed_lookup_should_not_be_cached()
    {
        var connectionManager = new FailingConnectionManager(_ => throw new TimeoutException("transient"));
        var persister = CreatePersister(connectionManager);

        Assert.ThrowsAsync<TimeoutException>(() => persister.GetSubscribers(messageType));
        Assert.ThrowsAsync<TimeoutException>(() => persister.GetSubscribers(messageType));

        Assert.That(connectionManager.Attempts, Is.EqualTo(2), "The second lookup should query the database again instead of returning the cached failure.");
    }

    [Test]
    public void Cancelled_lookup_should_not_be_cached()
    {
        using var firstCaller = new CancellationTokenSource();
        var connectionManager = new FailingConnectionManager(attempt =>
        {
            if (attempt == 1)
            {
                firstCaller.Cancel();
                throw new OperationCanceledException(firstCaller.Token);
            }

            throw new TimeoutException("reached the database");
        });
        var persister = CreatePersister(connectionManager);

        Assert.CatchAsync<OperationCanceledException>(() => persister.GetSubscribers(messageType, firstCaller.Token));

        // A different caller, that has not cancelled, should not observe the first caller's cancellation
        Assert.ThrowsAsync<TimeoutException>(() => persister.GetSubscribers(messageType, CancellationToken.None));
        Assert.That(connectionManager.Attempts, Is.EqualTo(2));
    }

    static SubscriptionPersister CreatePersister(IConnectionManager connectionManager) =>
        new(
            connectionManager: connectionManager,
            tablePrefix: "Prefix_",
            sqlDialect: new SqlDialect.MsSqlServer(),
            cacheFor: TimeSpan.FromMinutes(1));

    class FailingConnectionManager(Action<int> onAttempt) : IConnectionManager
    {
        int attempts;

        public int Attempts => attempts;

        public DbConnection BuildNonContextual()
        {
            onAttempt(Interlocked.Increment(ref attempts));
            throw new InvalidOperationException("onAttempt is expected to throw");
        }

        public DbConnection Build(IncomingMessage incomingMessage) => throw new NotSupportedException();
    }
}
