namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    public partial class DefaultServer
    {
        private partial void SetConnectionBuilder(PersistenceExtensions<SqlPersistence> sqlPersistence) =>
            sqlPersistence.ConnectionBuilder(MsSqlMicrosoftDataClientConnectionBuilder.Build);
    }
}