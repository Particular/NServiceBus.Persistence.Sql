using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using NServiceBus;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SubscriptionPersister : ISubscriptionStorage
{
    string connectionString;
    string schema;
    string endpointName;

    public SubscriptionPersister(string connectionString, string schema, string endpointName)
    {
        this.connectionString = connectionString;
        this.schema = schema;
        this.endpointName = endpointName;
    }

    public void Subscribe(Address client, IEnumerable<MessageType> messageTypes)
    {
        Subscribe(client.ToString(), messageTypes.Select(x => x.TypeName));
    }

    internal void Subscribe(string client, IEnumerable<string> messageTypes)
    {
        var commandText = string.Format(@"
IF NOT EXISTS 
(
    SELECT * FROM [{0}].[{1}.SubscriptionData]
    WHERE 
        Subscriber = @Subscriber AND
        MessageType = @MessageType
) 
BEGIN
    INSERT INTO [{0}].[{1}.SubscriptionData] 
    (
        Subscriber, 
        MessageType
    ) 
    VALUES 
    (
        @Subscriber, 
        @MessageType
    )
END", schema, endpointName);
        using (var connection = SqlHelpers.New(connectionString))
        {
            foreach (var messageType in messageTypes)
            {
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.AddParameter("MessageType", messageType);
                    command.AddParameter("Subscriber", client);
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    public void Unsubscribe(Address client, IEnumerable<MessageType> messageTypes)
    {
        Unsubscribe(client.ToString(), messageTypes.Select(x => x.TypeName));
    }

    internal void Unsubscribe(string client, IEnumerable<string> messageTypes)
    {
        var commandText = string.Format(@"
DELETE FROM [{0}].[{1}.SubscriptionData] 
WHERE 
    Subscriber = @Subscriber AND 
    MessageType = @MessageType", schema, endpointName);
        using (var connection = SqlHelpers.New(connectionString))
        {
            foreach (var messageType in messageTypes)
            {
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.AddParameter("MessageType", messageType);
                    command.AddParameter("Subscriber", client);
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    public IEnumerable<Address> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
    {
        return GetSubscriberAddressesForMessage(messageTypes.Select(x => x.TypeName))
            .Select(Address.Parse);
    }

    public IEnumerable<string> GetSubscriberAddressesForMessage(IEnumerable<string> messageTypes)
    {
        using (var command = new SqlCommand())
        {
            var builder = new StringBuilder();
            builder.AppendFormat(@"
SELECT DISTINCT Subscriber
FROM [{0}].[{1}.SubscriptionData] 
WHERE MessageType IN (", schema, endpointName);
            var types = messageTypes.ToList();
            for (var i = 0; i < types.Count; i++)
            {
                var messageType = types[i];
                var paramName = "@type" + i;
                builder.Append(paramName);
                if (i < types.Count - 1)
                {
                    builder.Append(", ");
                }
                command.AddParameter(paramName, messageType);
            }
            builder.Append(")");
            command.CommandText = builder.ToString();
            using (var connection = SqlHelpers.New(connectionString))
            {
                command.Connection = connection;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(0);
                    }
                }
            }
        }
    }

    public void Init()
    {
    }
}