using NServiceBus.Persistence.Sql;

[assembly: SqlPersistenceSettings(
        MsSqlServerScripts = true,
        ProduceSagaScripts = false)]
