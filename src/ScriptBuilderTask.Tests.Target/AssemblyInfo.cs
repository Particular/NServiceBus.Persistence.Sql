
using NServiceBus.Persistence.Sql;

[assembly: SqlPersistenceSettings(
    msSqlServerScripts: true,
    scriptPromotionPath: "$(SolutionDir)$(ProjectDir)Postfix")]