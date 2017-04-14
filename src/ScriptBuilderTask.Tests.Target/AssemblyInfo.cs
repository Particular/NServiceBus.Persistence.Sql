
using NServiceBus.Persistence.Sql;

[assembly: SqlPersistenceSettings(
    MsSqlServerScripts= true,
    ScriptPromotionPath= "$(SolutionDir)$(ProjectDir)Postfix")]