using System.Collections.Generic;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class Settings
{
    public List<BuildSqlDialect> BuildDialects { get; set; }
    public string ScriptPromotionPath { get; set; }
    public bool ProduceOutboxScripts { get; set; }
    public bool ProduceSubscriptionScripts { get; set; }
    public bool ProduceTimeoutScripts { get; set; }
    public bool ProduceSagaScripts { get; set; }
}