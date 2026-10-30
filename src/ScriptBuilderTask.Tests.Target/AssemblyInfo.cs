using NServiceBus.Persistence.Sql;

[assembly: SqlPersistenceSettings(
    MsSqlServerScripts = true,
    MySqlScripts = true,
    PostgreSqlScripts = true,
    OracleScripts = true,
    ProduceTimeoutScripts = false, // Saga scripts must still include the required timeout table.
    ScriptPromotionPath = "$(SolutionDir)$(ProjectDir)Postfix")]
