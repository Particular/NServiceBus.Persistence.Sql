using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NUnit.Framework;
using Particular.Approvals;

public abstract class SubscriptionPersisterTests
{
    BuildSqlDialect sqlDialect;
    string schema;
    Func<string, DbConnection> dbConnection;
    protected abstract Func<string, DbConnection> GetConnection();
    protected virtual bool SupportsSchemas() => true;
    string tablePrefix;

    public SubscriptionPersisterTests(BuildSqlDialect sqlDialect, string schema)
    {
        this.sqlDialect = sqlDialect;
        this.schema = schema;
    }

    SubscriptionPersister Setup(string theSchema)
    {
        dbConnection = GetConnection();
        tablePrefix = GetTablePrefix();
        var dialect = sqlDialect.Convert(theSchema);
        var persister = new SubscriptionPersister(
            connectionManager: new ConnectionManager(() => dbConnection(theSchema)),
            tablePrefix: $"{tablePrefix}_",
            sqlDialect: dialect,
            cacheFor: TimeSpan.FromSeconds(10));

        using (var connection = dbConnection(theSchema))
        {
            connection.Open();
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, schema: theSchema);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildCreateScript(sqlDialect), tablePrefix, schema: theSchema);
        }

        return persister;
    }

    protected virtual string GetTablePrefix()
    {
        return nameof(SubscriptionPersisterTests);
    }

    [TearDown]
    public void TearDown()
    {
        using (var connection = dbConnection(null))
        {
            connection.Open();
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, schema: null);
        }
        using (var connection = dbConnection(schema))
        {
            connection.Open();
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, schema: schema);
        }
    }

    [Test]
    public void ExecuteCreateTwice()
    {
        using (var connection = dbConnection(schema))
        {
            connection.Open();
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildCreateScript(sqlDialect), GetTablePrefix(), schema: schema);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildCreateScript(sqlDialect), GetTablePrefix(), schema: schema);
        }
    }

    [Test]
    public void Subscribe()
    {
        var persister = Setup(schema);

        var type1 = new MessageType("type1", new Version(0, 0, 0, 0));
        var type2 = new MessageType("type2", new Version(0, 0, 0, 0));
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type1, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type2, null).Await();
        persister.Subscribe(new Subscriber("e@machine2", "endpoint"), type1, null).Await();
        persister.Subscribe(new Subscriber("e@machine2", "endpoint"), type2, null).Await();
        persister.Subscribe(new Subscriber("e@machine3", null), type2, null).Await();
        var result = persister.GetSubscribers(new[] { type1, type2 }).Result.OrderBy(s => s.TransportAddress);
        Assert.That(result, Is.Not.Empty);
        Approver.Verify(result);
    }

    [Test]
    public void Subscribe_multiple_with_no_endpoint()
    {
        var persister = Setup(schema);

        var type = new MessageType("type", new Version(0, 0, 0, 0));
        persister.Subscribe(new Subscriber("e@machine1", null), type, null).Await();
        // Ensuring that MSSQL's handling of = null vs. is null doesn't cause a PK violation here
        persister.Subscribe(new Subscriber("e@machine1", null), type, null).Await();
        var result = persister.GetSubscribers(type).Result.OrderBy(s => s.TransportAddress);
        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Cached_get_should_be_faster()
    {
        var persister = Setup(schema);

        var type = new MessageType("type1", new Version(0, 0, 0, 0));
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type, null).Await();
        var first = Stopwatch.StartNew();
        var subscribersFirst = await persister.GetSubscribers(type)
            .ConfigureAwait(false);
        var firstTime = first.ElapsedMilliseconds;
        var second = Stopwatch.StartNew();
        var subscribersSecond = await persister.GetSubscribers(type)
            .ConfigureAwait(false);
        var secondTime = second.ElapsedMilliseconds;
        Assert.Multiple(() =>
        {
            Assert.That(secondTime * 1000, Is.LessThan(firstTime));
            Assert.That(subscribersSecond.Count(), Is.EqualTo(subscribersFirst.Count()));
        });
    }

    [Test]
    public void Should_be_cached()
    {
        var persister = Setup(schema);

        var type = new MessageType("type1", new Version(0, 0, 0, 0));
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type, null).Await();
        persister.GetSubscribers(type).Await();
        VerifyCache(persister.Cache);
    }

    static void VerifyCache(ConcurrentDictionary<string, SubscriptionPersister.CacheItem> cache, [CallerMemberName] string callerMemberName = null)
    {
        var items = cache
            .OrderBy(_ => _.Key)
            .ToDictionary(_ => _.Key,
                elementSelector: item =>
                {
                    return item.Value.Subscribers.Result
                        .OrderBy(_ => _.Endpoint)
                        .ThenBy(_ => _.TransportAddress);
                });
        Approver.Verify(items, callerMemberName: callerMemberName);
    }

    [Test]
    public void Subscribe_with_same_type_should_clear_cache()
    {
        var persister = Setup(schema);

        var matchingType = new MessageType("matchingType", new Version(0, 0, 0, 0));
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), matchingType, null).Await();
        persister.GetSubscribers(matchingType).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), matchingType, null).Await();
        VerifyCache(persister.Cache);
    }

    [Test]
    public void Unsubscribe_with_same_type_should_clear_cache()
    {
        var persister = Setup(schema);

        var matchingType = new MessageType("matchingType", new Version(0, 0, 0, 0));
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), matchingType, null).Await();
        persister.GetSubscribers(matchingType).Await();
        persister.Unsubscribe(new Subscriber("e@machine1", "endpoint"), matchingType, null).Await();
        VerifyCache(persister.Cache);
    }

    [Test]
    public void Unsubscribe_with_part_type_should_partially_clear_cache()
    {
        var persister = Setup(schema);

        var version = new Version(0, 0, 0, 0);
        var type1 = new MessageType("type1", version);
        var type2 = new MessageType("type2", version);
        var type3 = new MessageType("type3", version);
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type1, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type2, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type3, null).Await();

        persister.GetSubscribers(type1).Await();
        persister.GetSubscribers(type2).Await();
        persister.GetSubscribers(type3).Await();
        persister.GetSubscribers(new[] { type1, type2 }).Await();
        persister.GetSubscribers(new[] { type2, type3 }).Await();
        persister.GetSubscribers(new[] { type1, type3 }).Await();
        persister.GetSubscribers(new[] { type1, type2, type3 }).Await();
        persister.Unsubscribe(new Subscriber("e@machine1", "endpoint"), type2, null).Await();
        VerifyCache(persister.Cache);
    }

    [Test]
    public void Subscribe_duplicate_add()
    {
        var persister = Setup(schema);

        var type1 = new MessageType("type1", new Version(0, 0, 0, 0));
        var type2 = new MessageType("type2", new Version(0, 0, 0, 0));
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type1, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type2, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type1, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type2, null).Await();
        var result = persister.GetSubscribers(new[] { type1, type2 }).Result.ToList();
        Assert.That(result, Is.Not.Empty);
        Approver.Verify(result);
    }

    [Test]
    public void Subscribe_version_migration()
    {
        var persister = Setup(schema);
        var type1 = new MessageType("type1", new Version(0, 0, 0, 0));
        //NSB 5.x: endpoint is null
        persister.Subscribe(new Subscriber("e@machine1", null), type1, null).Await();
        //NSB 6.x: same subscriber now mentions endpoint
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type1, null).Await();
        var result = persister.GetSubscribers(type1).Result.ToList();
        Assert.That(result, Is.Not.Empty);
        Approver.Verify(result);
    }

    [Test]
    public void Subscribe_different_endpoint_name()
    {
        var persister = Setup(schema);
        var type1 = new MessageType("type1", new Version(0, 0, 0, 0));
        //NSB 6.x: old endpoint value
        persister.Subscribe(new Subscriber("e@machine1", "e1"), type1, null).Await();
        //NSB 6.x: same address, new endpoint value
        persister.Subscribe(new Subscriber("e@machine1", "e2"), type1, null).Await();
        var result = persister.GetSubscribers(type1).Result.ToList();
        Assert.That(result, Is.Not.Empty);
        Approver.Verify(result);
    }

    [Test]
    public void Subscribe_should_not_downgrade()
    {
        var persister = Setup(schema);
        var type1 = new MessageType("type1", new Version(0, 0, 0, 0));
        //NSB 6.x: subscriber contains endpoint
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type1, null).Await();
        //NSB 5.x: endpoint is null, don't want to remove endpoint value from table though
        persister.Subscribe(new Subscriber("e@machine1", null), type1, null).Await();
        var result = persister.GetSubscribers(type1).Result.ToList();
        Assert.That(result, Is.Not.Empty);
        Approver.Verify(result);
    }

    [Test]
    public void Unsubscribe()
    {
        var persister = Setup(schema);

        var message2 = new MessageType("type2", new Version(0, 0));
        var message1 = new MessageType("type1", new Version(0, 0));
        var address1 = new Subscriber("address1@machine1", "endpoint");
        persister.Subscribe(address1, message2, null).Await();
        persister.Subscribe(address1, message1, null).Await();
        var address2 = new Subscriber("address2@machine2", "endpoint");
        persister.Subscribe(address2, message2, null).Await();
        persister.Subscribe(address2, message1, null).Await();
        persister.Unsubscribe(address1, message2, null).Await();
        var result = persister.GetSubscribers(new[] { message2, message1 }).Result.ToList();
        Assert.That(result, Is.Not.Empty);
        Approver.Verify(result);
    }

    [Test]
    public void UseConfiguredSchema()
    {
        if (!SupportsSchemas())
        {
            Assert.Ignore();
        }

        var defaultSchemaPersister = Setup(null);
        var schemaPersister = Setup(schema);

        var type1 = new MessageType("type1", new Version(0, 0, 0, 0));
        defaultSchemaPersister.Subscribe(new Subscriber("e@machine1", "endpoint"), type1, null).Await();

        var result = schemaPersister.GetSubscribers(type1).Result.OrderBy(s => s.TransportAddress);
        Assert.That(result, Is.Empty);
    }
}