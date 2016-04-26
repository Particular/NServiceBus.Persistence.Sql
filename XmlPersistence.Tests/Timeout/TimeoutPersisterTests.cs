using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Timeout.Core;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
#if (!DEBUG)
[Explicit]
#endif
public class TimeoutPersisterTests
{
    static string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    static string endpointName = "Endpoint";
    TimeoutPersister persister;

    [SetUp]
    public void SetUp()
    {
        SetUpAsync().Await();
    }

    async Task SetUpAsync()
    {
        await DbBuilder.ReCreate(connectionString, endpointName);
        persister = new TimeoutPersister(connectionString, "dbo", endpointName);
    }

    [Test]
    public void TryRemove()
    {
        var timeout = new TimeoutData
        {
            Destination = "theDestination",
            SagaId = new Guid("ec1be111-39e5-403c-9960-f91282269455"),
            State = new byte[] {1},
            Time = new DateTime(2000, 1, 1),
            Headers = new Dictionary<string, string>
            {
                {"HeaderKey", "HeaderValue"}
            }
        };
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
            State = new byte[] {1},
            Time = new DateTime(2000, 1, 1),
            Headers = new Dictionary<string, string>
            {
                {"HeaderKey", "HeaderValue"}
            }
        };
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
            State = new byte[] {1},
            Time = timeout1Time,
            Headers = new Dictionary<string, string>()
        };
        persister.Add(timeout1, null).Await();
        var nextChunk = persister.Peek(timeout1.Id, null).Result;
        ObjectApprover.VerifyWithJson(nextChunk, s => s.Replace(timeout1.Id, "theId"));
    }


    [Test]
    public void GetNextChunk()
    {
        var startSlice = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        var timeout1Time = startSlice.AddSeconds(1);
        var timeout2Time = DateTime.UtcNow.AddSeconds(10);
        var timeout1 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] {1},
            Time = timeout1Time,
            Headers = new Dictionary<string, string>()
        };
        var timeout2 = new TimeoutData
        {
            Destination = "theDestination",
            State = new byte[] {1},
            Time = timeout2Time,
            Headers = new Dictionary<string, string>()
        };
        persister.Add(timeout1, null).Await();
        persister.Add(timeout2, null).Await();
        var nextChunk = persister.GetNextChunk(startSlice).Result;
        Assert.That(nextChunk.NextTimeToQuery, Is.EqualTo(timeout2Time).Within(TimeSpan.FromSeconds(1)));
        ObjectApprover.VerifyWithJson(nextChunk.DueTimeouts, s => s.Replace(timeout1.Id, "theId"));
    }
}