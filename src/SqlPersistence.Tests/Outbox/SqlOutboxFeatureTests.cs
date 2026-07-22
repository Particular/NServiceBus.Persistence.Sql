using NServiceBus.Settings;
using NUnit.Framework;

[TestFixture]
public class SqlOutboxFeatureTests
{
    [Test]
    public void Outbox_table_prefix_cleans_endpoint_name()
    {
        var settings = SettingsFor("My.Endpoint");

        Assert.That(SqlOutboxFeature.GetTablePrefix(settings), Is.EqualTo("My_Endpoint_"));
    }

    [Test]
    public void Outbox_table_prefix_honours_custom_table_prefix()
    {
        var settings = SettingsFor("My.Endpoint");
        settings.Set("SqlPersistence.TablePrefix", "Foo_");

        Assert.That(SqlOutboxFeature.GetTablePrefix(settings), Is.EqualTo("Foo_"));
    }

    [Test]
    public void Outbox_table_prefix_honours_processor_endpoint()
    {
        var settings = SettingsFor("My.Endpoint");
        settings.Set(SqlOutboxFeature.ProcessorEndpointKey, "Processor.Endpoint");

        // The outbox may run against a processor endpoint, so the prefix has to follow that
        // endpoint's name rather than this endpoint's.
        Assert.That(SqlOutboxFeature.GetTablePrefix(settings), Is.EqualTo("Processor_Endpoint_"));
    }

    static SettingsHolder SettingsFor(string endpointName)
    {
        var settings = new SettingsHolder();
        settings.Set("NServiceBus.Routing.EndpointName", endpointName);
        return settings;
    }
}
