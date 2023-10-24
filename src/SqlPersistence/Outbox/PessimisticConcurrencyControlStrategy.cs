using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

class PessimisticConcurrencyControlStrategy : ConcurrencyControlStrategy
{
    SqlDialect sqlDialect;
    OutboxCommands outboxCommands;

    public PessimisticConcurrencyControlStrategy(SqlDialect sqlDialect, OutboxCommands outboxCommands)
    {
        this.sqlDialect = sqlDialect;
        this.outboxCommands = outboxCommands;
    }

    public override async Task Begin(string messageId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
    {
        using (var command = sqlDialect.CreateCommand(connection))
        {
            command.CommandText = outboxCommands.PessimisticBegin;
            command.Transaction = transaction;

            command.AddParameter("MessageId", messageId);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);

            _ = await command.ExecuteNonQueryEx(cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task Complete(OutboxMessage outboxMessage, DbConnection connection, DbTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
    {
        string json = Serializer.Serialize(outboxMessage.TransportOperations.ToSerializable());
        json = sqlDialect.AddOutboxPadding(json);

        using (var command = sqlDialect.CreateCommand(connection))
        {
            command.CommandText = outboxCommands.PessimisticComplete;
            command.Transaction = transaction;

            command.AddParameter("MessageId", outboxMessage.MessageId);
            command.AddJsonParameter("Operations", json);

            _ = await command.ExecuteNonQueryEx(cancellationToken).ConfigureAwait(false);
        }
    }
}