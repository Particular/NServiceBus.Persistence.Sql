using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Timeout.Core;
using NUnit.Framework;

[TestFixture]
public class TimeoutPersisterTest
{
    [Test]
    public void TryRemove()
    {
        var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
        var endpointName = "Endpoint";
        TimeoutInstaller.Install(endpointName, connectionString);
        var persister = new TimeoutPersister(connectionString, "dbo", endpointName);
        var timeout = new TimeoutData
        {
            Destination = Address.Parse("theDestination"),
            OwningTimeoutManager = "theOwningTimeoutManager",
            SagaId = new Guid("ec1be111-39e5-403c-9960-f91282269455"),
            State = new byte[]{1},
            Time = new DateTime(2000,1,1),
            Headers = new Dictionary<string, string>
            {
                {"HeaderKey", "HeaderValue"}
            }
        };
        persister.Add(timeout);
        TimeoutData result;
        Assert.IsTrue(persister.TryRemove(timeout.Id, out result));
        ObjectApproval.ObjectApprover.VerifyWithJson(result,s => s.Replace(timeout.Id,"timeoutId"));
        Assert.IsFalse(persister.TryRemove(timeout.Id, out result));
        Assert.IsNull(result);
    }
}