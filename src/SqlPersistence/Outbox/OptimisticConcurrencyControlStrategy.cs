using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

class OptimisticConcurrencyControlStrategy : ConcurrencyControlStrategy
{
    SqlDialect sqlDialect;
    OutboxCommands outboxCommands;

    public OptimisticConcurrencyControlStrategy(SqlDialect sqlDialect, OutboxCommands outboxCommands)
    {
        this.sqlDialect = sqlDialect;
        this.outboxCommands = outboxCommands;
    }

    public override Task Begin(string messageId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public override async Task Complete(OutboxMessage outboxMessage, DbConnection connection, DbTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
    {
        string json = Serializer.Serialize(outboxMessage.TransportOperations.ToSerializable());
        json = sqlDialect.AddOutboxPadding(json);

        using (var command = sqlDialect.CreateCommand(connection))
        {
            command.CommandText = outboxCommands.OptimisticStore;
            command.Transaction = transaction;
            command.AddParameter("MessageId", outboxMessage.MessageId, 200);
            command.AddJsonParameter("Operations", json);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion, 23);
            await command.ExecuteNonQueryEx(cancellationToken).ConfigureAwait(false);
        }
    }
}