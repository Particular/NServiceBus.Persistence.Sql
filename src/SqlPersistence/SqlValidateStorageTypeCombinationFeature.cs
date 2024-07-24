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

    protected override void Setup(FeatureConfigurationContext context) => ValidateSagaOutboxCombo(context.Settings);

    // This check should normally be handled by Core but unfortunately there is a bug that prevents the check from executing
    // https://github.com/Particular/NServiceBus/issues/6378
    internal static void ValidateSagaOutboxCombo(IReadOnlySettings settings)
    {
        if (settings.IsFeatureActive(typeof(Outbox)) &&
            settings.IsFeatureActive(typeof(Sagas)) &&
            settings.IsFeatureActive(typeof(SqlSagaFeature)) != settings.IsFeatureActive(typeof(SqlOutboxFeature)))
        {
            throw new Exception("When both sagas and outbox are enabled, SQL persistence must be enabled for either both sagas and outbox, or neither.");
        }
    }
}
