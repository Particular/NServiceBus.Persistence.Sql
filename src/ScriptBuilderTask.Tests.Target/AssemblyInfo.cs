
using NServiceBus.Persistence.Sql;

[assembly: SqlPersistenceSettings(
    MsSqlServerScripts = true,
    MySqlScripts = true,
    OracleScripts = true,
    ScriptPromotionPath = "$(SolutionDir)$(ProjectDir)Postfix")]