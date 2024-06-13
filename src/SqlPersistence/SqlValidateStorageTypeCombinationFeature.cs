using System;
using NServiceBus.Features;
using NServiceBus.Settings;

sealed class SqlValidateStorageTypeCombinationFeature : Feature
{
    public SqlValidateStorageTypeCombinationFeature()
    {
        EnableByDefault();
        DependsOnOptionally<SqlOutboxFeature>();
        DependsOnOptionally<SqlSagaFeature>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        ValidateSagaOutboxCombo(settings);
    }

    // This check should normally be handled by Core but unfortunately there is a bug that prevents the check from executing
    // https://github.com/Particular/NServiceBus/issues/6378
    internal static void ValidateSagaOutboxCombo(IReadOnlySettings settings)
    {
        var isOutboxEnabled = settings.IsFeatureActive(typeof(Outbox));
        var isSagasEnabled = settings.IsFeatureActive(typeof(Sagas));
        if (!isOutboxEnabled || !isSagasEnabled)
        {
            return;
        }
        var isSagasEnabledForSqlPersistence = settings.IsFeatureActive(typeof(SqlSagaFeature));
        var isOutboxEnabledForSqlPersistence = settings.IsFeatureActive(typeof(SqlOutboxFeature));
        if ((isSagasEnabledForSqlPersistence && isOutboxEnabledForSqlPersistence)
            || (!isSagasEnabledForSqlPersistence && !isOutboxEnabledForSqlPersistence))
        {
            return;
        }
        throw new Exception("Sql Persistence must be enabled for either both Sagas and Outbox, or neither.");
    }
}