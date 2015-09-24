using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Timeout.Core;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
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
                Destination = Address.Parse("theDestination"),
                SagaId = new Guid("ec1be111-39e5-403c-9960-f91282269455"),
                State = new byte[] {1},
                Time = new DateTime(2000, 1, 1),
                Headers = new Dictionary<string, string>
                {
                    {"HeaderKey", "HeaderValue"}
                }
            };
            persister.Add(timeout);
            TimeoutData result;
            Assert.IsTrue(persister.TryRemove(timeout.Id, out result));
            ObjectApprover.VerifyWithJson(result, s => s.Replace(timeout.Id, "timeoutId"));
            Assert.IsFalse(persister.TryRemove(timeout.Id, out result));
            Assert.IsNull(result);
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
                Destination = Address.Parse("theDestination"),
                SagaId = sagaId,
                State = new byte[] {1},
                Time = new DateTime(2000, 1, 1),
                Headers = new Dictionary<string, string>
                {
                    {"HeaderKey", "HeaderValue"}
                }
            };
            persister.Add(timeout);
            persister.Add(timeout);
            TimeoutData result;
            persister.RemoveTimeoutBy(sagaId);
            Assert.IsFalse(persister.TryRemove(timeout.Id, out result));
            Assert.IsNull(result);
        }
    }

    [Test]
    public void GetNextChunk()
    {
        using (var testDatabase = new TimeoutDatabase())
        {
            var persister = testDatabase.Persister;
            var startSlice = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var timeout1Time = startSlice.AddSeconds(1);
            var timeout2Time = DateTime.UtcNow.AddSeconds(10);
            var timeout1 = new TimeoutData
            {
                Destination = Address.Parse("theDestination"),
                State = new byte[] {1},
                Time = timeout1Time,
                Headers = new Dictionary<string, string>()
            };
            var timeout2 = new TimeoutData
            {
                Destination = Address.Parse("theDestination"),
                State = new byte[] {1},
                Time = timeout2Time,
                Headers = new Dictionary<string, string>()
            };
            persister.Add(timeout1);
            persister.Add(timeout2);
            DateTime nextTime;
            var nextChunk = persister.GetNextChunk(startSlice, out nextTime);
            Assert.That(nextTime, Is.EqualTo(timeout2Time).Within(TimeSpan.FromSeconds(1)));
            ObjectApprover.VerifyWithJson(nextChunk, s => s.Replace(timeout1.Id, "theId"));
        }
    }
}