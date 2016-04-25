using System.Collections.Generic;
using NServiceBus.Outbox;
using NUnit.Framework;


[TestFixture]
#if (!DEBUG)
[Explicit]
#endif
public class OutboxPersisterTests
{
    [Test]
    public void Subscribe()
    {
        using (var testDatabase = new OutboxDatabase())
        {
            var persister = testDatabase.Persister;

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
                body: new byte[] {0x20,0x21},
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

}