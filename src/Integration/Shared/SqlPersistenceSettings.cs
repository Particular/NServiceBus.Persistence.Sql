using NServiceBus.Persistence.Sql;

[assembly: SqlPersistenceSettings(
    MsSqlServerScripts = true,
    MySqlScripts = true,
    OracleScripts = true,
    PostgreSqlScripts = true,
    ScriptPromotionPath = @"$(SolutionDir)Integration\PromotedSqlScripts")]
