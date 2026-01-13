#nullable enable

using System.Collections.Generic;
using NServiceBus.Persistence.Sql.ScriptBuilder;

sealed class Settings
{
    public required IReadOnlyCollection<BuildSqlDialect> BuildDialects { get; init; }

    public string? ScriptPromotionPath { get; init; }

    public required bool ProduceOutboxScripts { get; init; }

    public required bool ProduceSubscriptionScripts { get; init; }

    public required bool ProduceTimeoutScripts { get; init; }

    public required bool ProduceSagaScripts { get; init; }

    public required IReadOnlyCollection<SagaDefinition> SagaDefinitions { get; init; }
}