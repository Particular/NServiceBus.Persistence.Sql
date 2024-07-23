using System;
using NServiceBus.Features;
using NServiceBus.Settings;
using NUnit.Framework;

[TestFixture]
public class SqlValidateStorageTypeCombinationFeatureTests
{
    [TestCase(FeatureState.Active, FeatureState.Active, FeatureState.Active, FeatureState.Active)]
    [TestCase(FeatureState.Active, FeatureState.Deactivated, FeatureState.Active, FeatureState.Active)]
    [TestCase(FeatureState.Deactivated, FeatureState.Active, FeatureState.Active, FeatureState.Active)]
    [TestCase(FeatureState.Deactivated, FeatureState.Deactivated, FeatureState.Active, FeatureState.Active)]
    [TestCase(FeatureState.Deactivated, FeatureState.Deactivated, FeatureState.Deactivated, FeatureState.Deactivated)]
    [TestCase(FeatureState.Deactivated, FeatureState.Deactivated, FeatureState.Active, FeatureState.Deactivated)]
    [TestCase(FeatureState.Deactivated, FeatureState.Deactivated, FeatureState.Deactivated, FeatureState.Active)]
    [TestCase(FeatureState.Active, FeatureState.Active, FeatureState.Deactivated, FeatureState.Deactivated)]
    public void Should_not_throw_when_valid_combination(FeatureState outboxEnabled, FeatureState sagaEnabled, FeatureState outboxUsingSqlPersistence, FeatureState sagaUsingSqlPersistence)
    {
        var settings = new SettingsHolder();
        settings.Set(typeof(Outbox).FullName, outboxEnabled);
        settings.Set(typeof(Sagas).FullName, sagaEnabled);
        settings.Set(typeof(SqlSagaFeature).FullName, outboxUsingSqlPersistence);
        settings.Set(typeof(SqlOutboxFeature).FullName, sagaUsingSqlPersistence);

        Assert.DoesNotThrow(() => SqlValidateStorageTypeCombinationFeature.ValidateSagaOutboxCombo(settings));
    }

    [TestCase(FeatureState.Active, FeatureState.Active, FeatureState.Active, FeatureState.Deactivated)]
    [TestCase(FeatureState.Active, FeatureState.Active, FeatureState.Deactivated, FeatureState.Active)]
    public void Should_throw_when_not_valid_combination(FeatureState outboxEnabled, FeatureState sagaEnabled, FeatureState outboxUsingSqlPersistence, FeatureState sagaUsingSqlPersistence)
    {
        var settings = new SettingsHolder();
        settings.Set(typeof(Outbox).FullName, outboxEnabled);
        settings.Set(typeof(Sagas).FullName, sagaEnabled);
        settings.Set(typeof(SqlSagaFeature).FullName, outboxUsingSqlPersistence);
        settings.Set(typeof(SqlOutboxFeature).FullName, sagaUsingSqlPersistence);

        var ex = Assert.Throws<Exception>(() => SqlValidateStorageTypeCombinationFeature.ValidateSagaOutboxCombo(settings));
        Assert.AreEqual("When both sagas and outbox are enabled, SQL persistence must be enabled for either both sagas and outbox, or neither.", ex.Message);
    }
}
