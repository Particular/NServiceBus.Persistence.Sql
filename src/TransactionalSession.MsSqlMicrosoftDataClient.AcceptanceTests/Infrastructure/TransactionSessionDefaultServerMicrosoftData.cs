namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    public partial class TransactionSessionDefaultServer
    {
        private partial void SetConnectionBuilder(PersistenceExtensions<SqlPersistence> sqlPersistence) =>
            sqlPersistence.ConnectionBuilder(MsSqlMicrosoftDataClientConnectionBuilder.Build);
    }
}