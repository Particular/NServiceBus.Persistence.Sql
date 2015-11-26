using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Unicast.Subscriptions;
using NUnit.Framework;
using ObjectApproval;


[TestFixture]
#if (!DEBUG)
[Explicit]
#endif
public class SubscriptionPersisterTest
{
    [Test]
    public void Subscribe()
    {
        using (var testDatabase = new SubscriptionDatabase())
        {
            var persister = testDatabase.Persister;
            var messageTypes = new List<MessageType>
            {
                new MessageType("type1", new Version(0, 0, 0, 0)),
                new MessageType("type2", new Version(0, 0, 0, 0)),
            };
            persister.Subscribe("address1@machine1".ToSubscriber(), messageTypes, null).Await();
            persister.Subscribe("address2@machine2".ToSubscriber(), messageTypes, null).Await();
            var result = persister.GetSubscriberAddressesForMessage(messageTypes, null).Result.Select(x=>x.ToAddress());
            ObjectApprover.VerifyWithJson(result);
        }
    }

    [Test]
    public void Subscribe_duplicate_add()
    {
        using (var testDatabase = new SubscriptionDatabase())
        {
            var persister = testDatabase.Persister;
            var messageTypes = new List<MessageType>
            {
                new MessageType("type1", new Version(0, 0, 0, 0)),
                new MessageType("type2", new Version(0, 0, 0, 0)),
            };
            persister.Subscribe("address1@machine1".ToSubscriber(), messageTypes, null).Await();
            persister.Subscribe("address1@machine1".ToSubscriber(), messageTypes, null).Await();
            var result = persister.GetSubscriberAddressesForMessage(messageTypes, null).Result.Select(x => x.ToAddress());
            ObjectApprover.VerifyWithJson(result);
        }
    }

    [Test]
    public void Unsubscribe()
    {
        using (var testDatabase = new SubscriptionDatabase())
        {
            var persister = testDatabase.Persister;
            var message2 = new MessageType("type2", new Version(0, 0));
            var message1 = new MessageType("type1", new Version(0, 0));
            var messageTypes = new List<MessageType>
            {
                message2,
                message1,
            };
            var address1 = "address1@machine1".ToSubscriber();
            persister.Subscribe(address1, messageTypes, null).Await();
            var address2 = "address2@machine2".ToSubscriber();
            persister.Subscribe(address2, messageTypes, null).Await();
            persister.Unsubscribe(address1, new List<MessageType> {message2}, null).Await();
            var result = persister.GetSubscriberAddressesForMessage(messageTypes, null).Result.Select(x => x.ToAddress());
            ObjectApprover.VerifyWithJson(result);
        }
    }
}