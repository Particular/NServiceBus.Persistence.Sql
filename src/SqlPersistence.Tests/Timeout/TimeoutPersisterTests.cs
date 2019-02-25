using System;
using System.Collections.Generic;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Timeout.Core;
using NUnit.Framework;
using Particular.Approvals;

public abstract class TimeoutPersisterTests
{
    BuildSqlDialect sqlDialect;
    string schema;
    Func<string, DbConnection> dbConnection;
    static DateTime goodUtcNowValue = new DateTime(2017, 10, 24, 10, 54, 15, DateTimeKind.Utc);
    protected abstract Func<string, DbConnection> GetConnection();
    protected virtual bool SupportsSchemas() => true;

    public TimeoutPersisterTests(BuildSqlDialect sqlDialect, string schema)
    {
        this.sqlDialect = sqlDialect;
        this.schema = schema;
        dbConnection = GetConnection();
    }

    TimeoutPersister Setup(string theSchema, TimeSpan? cleanupInterval = null)
    {
        var name = GetTablePrefix();
        using (var connection = dbConnection(theSchema))
        {
            connection.Open();
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlDialect), name, schema: theSchema);
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlDialect), name, schema: theSchema);
        }

        var preventCleanupInterval = goodUtcNowValue - new DateTime() + TimeSpan.FromDays(1); //Prevents entering cleanup mode right away (load timeouts from beginning of time)

        return new TimeoutPersister(
            connectionManager: new SingleTenantConnectionManager(() => dbConnection(theSchema)),
            tablePrefix: $"{name}_",
            sqlDialect: sqlDialect.Convert(theSchema),
            timeoutsCleanupExecutionInterval: cleanupInterval ?? preventCleanupInterval,
            utcNow: () => goodUtcNowValue);
    }

    [TearDown]
    public void TearDown()
    {
        var name = GetTablePrefix();
        using (var connection = dbConnection(null))
        {
            connection.Open();
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlDialect), name, schema: null);
        }
        using (var connection = dbConnection(schema))
        {
            connection.Open();
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlDialect), name, schema: schema);
        }
    }

    protected virtual string GetTablePrefix()
    {
        return $"{nameof(TimeoutPersisterTests)}{TestContext.CurrentContext.Test.Name}";
    }

    [Test]
    public void ExecuteCreateTwice()
    {
        var name = GetTablePrefix();
        using (var connection = dbConnection(schema))
        {
            connection.Open();
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlDialect), name, schema: schema);
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlDialect), name, schema: schema);
        }
    }

    [Test]
    public void TryRemove()
    {
        var timeout = new TimeoutData
        {
            Destination = "theDestination",
            SagaId = new Guid("ec1be111-39e5-403c-9960-f91282269455"),
            State = new byte[] { 1 },
            Time = new DateTime(2000, 1, 1),
            Headers = new Dictionary<string, string>
            {
                {"HeaderKey", "HeaderValue"}
            }
        };
        var persister = Setup(schema);
        persister.Add(timeout, null).Await();
        Assert.IsTrue(persister.TryRemove(timeout.Id, null).Result);
        Assert.IsFalse(persister.TryRemove(timeout.Id, null).Result);
    }

    [Test]
    public void RemoveTimeoutBy()
    {
        var sagaId = new Guid("ec1be111-39e5-403c-9960-f91282269455");
        var timeout = new TimeoutData
        {
            Destination = "theDestination",
            SagaId = sagaId,
            State = new byte[] { 1 },
            Time = new DateTime(2000, 1, 1),
            Headers = new Dictionary<string, string>
            {
                {"HeaderKey", "HeaderValue"}
            }
        };
        var persister = Setup(schema);
        persister.Add(timeout, null).Await();
        persister.RemoveTimeoutBy(sagaId, null).Await();
        Assert.IsFalse(persister.TryRemove(timeout.Id, null).Result);
    }

    [Test]
    public void Peek()
    {
        var startSlice = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        var timeout1Time = startSlice.AddSeconds(1);
        var timeout1 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] { 1 },
            Time = timeout1Time,
            Headers = new Dictionary<string, string>()
        };
        var persister = Setup(schema);
        persister.Add(timeout1, null).Await();
        var nextChunk = persister.Peek(timeout1.Id, null).Result;
        Assert.IsNotNull(nextChunk);
        Assert.AreEqual(DateTimeKind.Utc, nextChunk.Time.Kind);
        Approver.Verify(nextChunk, s => s.Replace(timeout1.Id, "theId"));
    }


    [Test]
    public void GetNextChunk()
    {
        var startSlice = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        var timeout1Time = startSlice.AddSeconds(1);
        var timeout2Time = goodUtcNowValue.AddSeconds(10);
        var timeout1 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] { 1 },
            Time = timeout1Time,
            Headers = new Dictionary<string, string>()
        };
        var timeout2 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] { 1 },
            Time = timeout2Time,
            Headers = new Dictionary<string, string>()
        };
        var persister = Setup(schema);
        persister.Add(timeout1, null).Await();
        persister.Add(timeout2, null).Await();
        var nextChunk = persister.GetNextChunk(startSlice).Result;
        Assert.That(nextChunk.NextTimeToQuery, Is.EqualTo(timeout2Time).Within(TimeSpan.FromSeconds(1)));
        Assert.IsNotNull(nextChunk);
        Assert.AreEqual(DateTimeKind.Utc, nextChunk.NextTimeToQuery.Kind);
        Approver.Verify(nextChunk, s => s.Replace(timeout1.Id, "theId"));
    }

    [Test]
    public void GetNextChunk_LowerBound()
    {
        var startSlice = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        var timeout1Time = startSlice;
        var timeout1 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] { 1 },
            Time = timeout1Time,
            Headers = new Dictionary<string, string>()
        };
        var persister = Setup(schema);
        persister.Add(timeout1, null).Await();
        var nextChunk = persister.GetNextChunk(startSlice).Result;
        Assert.IsNotNull(nextChunk);
        Approver.Verify(nextChunk, s => s.Replace(timeout1.Id, "theId"));
    }

    [Test]
    public void GetNextChunk_Cleanup()
    {
        var startSlice = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        var timeout1Time = startSlice.AddYears(-10);
        var timeout1 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] { 1 },
            Time = timeout1Time,
            Headers = new Dictionary<string, string>()
        };
        var persister = Setup(schema, TimeSpan.FromMinutes(2));
        persister.Add(timeout1, null).Await();
        var nextChunk = persister.GetNextChunk(startSlice).Result;
        Assert.IsNotNull(nextChunk);
        Approver.Verify(nextChunk, s => s.Replace(timeout1.Id, "theId"));
    }

    [Test]
    public void GetNextChunk_CleanupOnce()
    {
        var startSlice = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        var timeout1Time = startSlice.AddYears(-10);
        var timeout1 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] { 1 },
            Time = timeout1Time,
            Headers = new Dictionary<string, string>()
        };
        var persister = Setup(schema, TimeSpan.FromMinutes(2));
        persister.Add(timeout1, null).Await();

        //Call once triggering clean-up mode
        persister.GetNextChunk(startSlice).GetAwaiter().GetResult();

        //Call again to check if clean-up mode is now disabled -- expect no results
        var nextChunk = persister.GetNextChunk(startSlice).Result;

        Assert.IsNotNull(nextChunk);
        Approver.Verify(nextChunk, s => s.Replace(timeout1.Id, "theId"));
    }

    [Test]
    public void FractionalSeconds()
    {
        var startSlice = new DateTime(2000, 1, 1, 1, 1, 1, 42, DateTimeKind.Utc); // 42ms
        var timeout1Time = startSlice.AddSeconds(1);
        var timeout2Time = goodUtcNowValue.AddSeconds(10);
        var timeout1 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] { 1 },
            Time = timeout1Time,
            Headers = new Dictionary<string, string>()
        };
        var timeout2 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] { 1 },
            Time = timeout2Time,
            Headers = new Dictionary<string, string>()
        };
        var persister = Setup(schema);
        persister.Add(timeout1, null).Await();
        persister.Add(timeout2, null).Await();
        var nextChunk = persister.GetNextChunk(startSlice).Result;
        Assert.That(nextChunk.NextTimeToQuery, Is.EqualTo(timeout2Time).Within(TimeSpan.FromSeconds(1)));
        Assert.That(nextChunk.DueTimeouts.Length, Is.EqualTo(1));

        // MSSQL stores 42ms as .043 because it doesn't have millisecond precision. MySQL/Oracle store to nearest second.
        Assert.That(nextChunk.DueTimeouts[0].DueTime, Is.EqualTo(timeout1Time).Within(TimeSpan.FromMilliseconds(43)));
    }

    [Test]
    public void UseConfiguredSchema()
    {
        if (!SupportsSchemas())
        {
            Assert.Ignore();
        }

        var startSlice = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        var timeout1Time = startSlice.AddSeconds(1);
        var timeout1 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] { 1 },
            Time = timeout1Time,
            Headers = new Dictionary<string, string>()
        };
        var defaultSchemaPersister = Setup(null);
        var schemaPersister = Setup(schema);
        defaultSchemaPersister.Add(timeout1, null).Await();

        var result = schemaPersister.Peek(timeout1.Id, null).Result;
        Assert.Null(result);
    }
}