using System.Collections.Generic;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SubscriptionPersisterTest
{
    [Test]
    public void Subscribe()
    {
        using (var testDatabase = new TestDatabase())
        {
            var persister = testDatabase.SubscriptionPersister;
            var messageTypes = new List<string>
            {
                "type1",
                "type2"
            };
            persister.Subscribe("address1@machine1", messageTypes);
            persister.Subscribe("address2@machine2", messageTypes);
            var result = persister.GetSubscriberAddressesForMessage(messageTypes);
            ObjectApprover.VerifyWithJson(result);
        }
    }

    [Test]
    public void Subscribe_duplicate_add()
    {
        using (var testDatabase = new TestDatabase())
        {
            var persister = testDatabase.SubscriptionPersister;
            var messageTypes = new List<string>
            {
                "type1",
                "type2"
            };
            persister.Subscribe("address1@machine1", messageTypes);
            persister.Subscribe("address1@machine1", messageTypes);
            var result = persister.GetSubscriberAddressesForMessage(messageTypes);
            ObjectApprover.VerifyWithJson(result);
        }
    }

    [Test]
    public void Unsubscribe()
    {
        using (var testDatabase = new TestDatabase())
        {
            var persister = testDatabase.SubscriptionPersister;
            var message2 = "type2";
            var message1 = "type1";
            var messageTypes = new List<string>
            {
                message2,
                message1,
            };
            var address1 = "address1@machine1";
            persister.Subscribe(address1, messageTypes);
            var address2 = "address2@machine2";
            persister.Subscribe(address2, messageTypes);
            persister.Unsubscribe(address1, new List<string> {message2});
            var result = persister.GetSubscriberAddressesForMessage(messageTypes);
            ObjectApprover.VerifyWithJson(result);
        }
    }
}