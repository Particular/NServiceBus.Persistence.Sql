using System;
using System.Collections.Generic;
using NServiceBus.Timeout.Core;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
#if (!DEBUG)
[Explicit]
#endif
public class TimeoutPersisterTest
{
    [Test]
    public void TryRemove()
    {
        using (var testDatabase = new TimeoutDatabase())
        {
            var persister = testDatabase.Persister;
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
            persister.Add(timeout,null).Await();;
            Assert.IsTrue(persister.TryRemove(timeout.Id, null).Result);
            Assert.IsFalse(persister.TryRemove(timeout.Id, null).Result);
        }
    }

    [Test]
    public void RemoveTimeoutBy()
    {
        using (var testDatabase = new TimeoutDatabase())
        {
            var persister = testDatabase.Persister;
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
            persister.Add(timeout, null).Await();;
            persister.RemoveTimeoutBy(sagaId, null).Await();;
            Assert.IsFalse(persister.TryRemove(timeout.Id, null).Result);
        }
    }

    [Test]
    public void Peek()
    {
        using (var testDatabase = new TimeoutDatabase())
        {
            var persister = testDatabase.Persister;
            var startSlice = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var timeout1Time = startSlice.AddSeconds(1);
            var timeout1 = new TimeoutData
            {
                Destination = "theDestination",
                State = new byte[] { 1 },
                Time = timeout1Time,
                Headers = new Dictionary<string, string>()
            };
            persister.Add(timeout1, null).Await();;
            var nextChunk = persister.Peek(timeout1.Id, null).Result;
            ObjectApprover.VerifyWithJson(nextChunk, s => s.Replace(timeout1.Id, "theId"));
        }
    }
}