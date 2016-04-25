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


    public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        using (var connection = await SqlHelpers.New(connectionString))
        {
            await Subscribe(subscriber, connection, messageType);
        }
    }

    async Task Subscribe(Subscriber subscriber, SqlConnection connection, MessageType messageType)
    {
        using (var command = new SqlCommand(subscribeCommandText, connection))
        {
            command.AddParameter("MessageType", messageType.TypeName);
            command.AddParameter("Subscriber", subscriber.ToAddress());
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            await command.ExecuteNonQueryEx();
        }
    }


    public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        using (var connection = await SqlHelpers.New(connectionString))
        {
            await Unsubscribe(subscriber, connection, messageType);
        }
    }

    async Task Unsubscribe(Subscriber subscriber, SqlConnection connection, MessageType messageType)
    {
        using (var command = new SqlCommand(unsubscribeCommandText, connection))
        {
            command.AddParameter("MessageType", messageType.TypeName);
            command.AddParameter("Subscriber", subscriber.ToAddress());
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
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
                    return addresses.Select(s => s.ToSubscriber());
                }
            }
        }
    }

}