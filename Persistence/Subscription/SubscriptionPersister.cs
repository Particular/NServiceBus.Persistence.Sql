using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SubscriptionPersister : ISubscriptionStorage
{
    string connectionString;
    string schema;
    string endpointName;
    string subscribeCommandText;
    string unsubscribeCommandText;

    public SubscriptionPersister(string connectionString, string schema, string endpointName)
    {
        this.connectionString = connectionString;
        this.schema = schema;
        this.endpointName = endpointName;
        subscribeCommandText = string.Format(@"
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
        MessageType,
        PersistenceVersion
    ) 
    VALUES 
    (
        @Subscriber, 
        @MessageType, 
        @PersistenceVersion
    )
END", schema, endpointName);
        unsubscribeCommandText = $@"
DELETE FROM [{schema}].[{endpointName}.SubscriptionData] 
WHERE 
    Subscriber = @Subscriber AND 
    MessageType = @MessageType";
    }


    public async Task Subscribe(string client, IEnumerable<MessageType> messageTypes, ContextBag context)
    {
        using (var connection = await SqlHelpers.New(connectionString))
        {
            foreach (var messageType in messageTypes)
            {
                using (var command = new SqlCommand(subscribeCommandText, connection))
                {
                    command.AddParameter("MessageType", messageType.TypeName);
                    command.AddParameter("Subscriber", client);
                    command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
                    await command.ExecuteNonQueryEx();
                }
            }
        }
    }


    public async Task Unsubscribe(string client, IEnumerable<MessageType> messageTypes, ContextBag context)
    {
        using (var connection = await SqlHelpers.New(connectionString))
        {
            foreach (var messageType in messageTypes)
            {
                using (var command = new SqlCommand(unsubscribeCommandText, connection))
                {
                    command.AddParameter("MessageType", messageType.TypeName);
                    command.AddParameter("Subscriber", client);
                    await command.ExecuteNonQueryEx();
                }
            }
        }
    }


    public async Task<IEnumerable<string>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
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
                command.AddParameter(paramName, messageType.TypeName);
            }
            builder.Append(")");
            command.CommandText = builder.ToString();
            using (var connection = await SqlHelpers.New(connectionString))
            {
                command.Connection = connection;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var addresses = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        addresses.Add(reader.GetString(0));
                    }
                    return addresses;
                }
            }
        }
    }

}