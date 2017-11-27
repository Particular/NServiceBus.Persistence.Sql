using System;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Settings;
using NUnit.Framework;

[TestFixture]
public class OutboxSettingsExtensionsTests
{
    [Test]
    public void It_defaults_to_7_days_retention()
    {
        var settings = new SettingsHolder();

        var retention = settings.GetTimeToKeepDeduplicationData();

        Assert.AreEqual(TimeSpan.FromDays(7), retention);
    }

    [Test]
    public void It_uses_specified_retention_value()
    {
        var config = new EndpointConfiguration("MyEndpoint");
        config.EnableOutbox().TimeToKeepDeduplicationData(TimeSpan.FromDays(5));
        
        var retention = config.GetSettings().GetTimeToKeepDeduplicationData();

        Assert.AreEqual(TimeSpan.FromDays(5), retention);
    }

    [Test]
    public void It_defaults_to_1_minute_interval()
    {
        var settings = new SettingsHolder();

        var interval = settings.GetDeduplicationDataCleanupInterval();

        Assert.AreEqual(TimeSpan.FromMinutes(1), interval);
    }

    [Test]
    public void It_uses_specified_interval_value()
    {
        var config = new EndpointConfiguration("MyEndpoint");
        config.EnableOutbox().DeduplicationDataCleanupInterval(TimeSpan.FromMinutes(5));

        var interval = config.GetSettings().GetDeduplicationDataCleanupInterval();

        Assert.AreEqual(TimeSpan.FromMinutes(5), interval);
    }

    [Test]
    public void It_defaults_to_10000_items_batch()
    {
        var settings = new SettingsHolder();

        var batchSize = settings.GetDeduplicationDataCleanupBatchSize();

        Assert.AreEqual(10000, batchSize);
    }

    [Test]
    public void It_uses_specified_batch_size_value()
    {
        var config = new EndpointConfiguration("MyEndpoint");
        config.EnableOutbox().DeduplicationDataCleanupBatchSize(1000);

        var batchSize = config.GetSettings().GetDeduplicationDataCleanupBatchSize();

        Assert.AreEqual(1000, batchSize);
    }
}