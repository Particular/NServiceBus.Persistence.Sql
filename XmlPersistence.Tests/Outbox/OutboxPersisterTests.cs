using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Outbox;
using NUnit.Framework;

[TestFixture]
#if (!DEBUG)
[Explicit]
#endif
public class OutboxPersisterTests
{


    static string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    static string endpointName = "Endpoint";
    OutboxPersister persister;

    [SetUp]
    public void SetUp()
    {
        SetUpAsync().Await();
    }

    async Task SetUpAsync()
    {
        await DbBuilder.ReCreate(connectionString, endpointName);
        persister = new OutboxPersister(connectionString, "dbo", endpointName);
    }

    [Test]
    public void Subscribe()
    {
        var operations = new List<TransportOperation>
        {
            new TransportOperation(
                messageId: "Id1",
                options: new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                },
                body: new byte[] {0x20, 0x21},
                headers: new Dictionary<string, string>
                {
                    {
                        "HeaderKey1", "HeaderValue1"
                    }
                }
                )
        };
        //TODO:
        //persister.Store(new OutboxMessage("a", operations),).Await();
        //persister.Subscribe("address1@machine1".ToSubscriber(), type2, null).Await();
        //persister.Subscribe("address2@machine2".ToSubscriber(), type1, null).Await();
        //persister.Subscribe("address2@machine2".ToSubscriber(), type2, null).Await();
        //var result = persister.GetSubscriberAddressesForMessage(messageTypes, null).Result.Select(x => x.ToAddress());
        //ObjectApprover.VerifyWithJson(result);
    }
}