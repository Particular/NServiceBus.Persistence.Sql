using NServiceBus;
using NServiceBus.Settings;
using NUnit.Framework;

[TestFixture]
public class SqlOutboxFeatureTests
{
    [Test]
    public void Outbox_manifest_prefix_cleans_endpoint_name()
    {
        var settings = SettingsFor("My.Endpoint");
        var manifest = ManifestFor(settings);

        SqlOutboxFeature.ConfigureOutboxManifest(manifest, settings, new SqlDialect.MsSqlServer());

        Assert.That(manifest.Outbox.TableName, Is.EqualTo("[dbo].[My_Endpoint_OutboxData]"));
    }

    [Test]
    public void Outbox_manifest_prefix_honours_custom_table_prefix()
    {
        var settings = SettingsFor("My.Endpoint");
        settings.Set("SqlPersistence.TablePrefix", "Foo_");
        var manifest = ManifestFor(settings);

        SqlOutboxFeature.ConfigureOutboxManifest(manifest, settings, new SqlDialect.MsSqlServer());

        Assert.That(manifest.Outbox.TableName, Is.EqualTo("[dbo].[Foo_OutboxData]"));
    }

    [Test]
    public void Outbox_manifest_prefix_honours_processor_endpoint()
    {
        var settings = SettingsFor("My.Endpoint");
        settings.Set(SqlOutboxFeature.ProcessorEndpointKey, "Processor.Endpoint");
        var manifest = ManifestFor(settings);

        SqlOutboxFeature.ConfigureOutboxManifest(manifest, settings, new SqlDialect.MsSqlServer());

        // The outbox may run against a processor endpoint, and the manifest has to report the table
        // the runtime actually uses rather than the one derived from this endpoint's name.
        Assert.That(manifest.Outbox.TableName, Is.EqualTo("[dbo].[Processor_Endpoint_OutboxData]"));
    }

    [Test]
    public void Outbox_manifest_table_name_matches_runtime_table_name()
    {
        var settings = SettingsFor("My.Endpoint");
        var dialect = new SqlDialect.MsSqlServer();
        var manifest = ManifestFor(settings);

        SqlOutboxFeature.ConfigureOutboxManifest(manifest, settings, dialect);

        var runtime = dialect.GetOutboxTableName(SqlOutboxFeature.GetTablePrefix(settings));

        Assert.That(manifest.Outbox.TableName, Is.EqualTo(runtime));
    }

    static SettingsHolder SettingsFor(string endpointName)
    {
        var settings = new SettingsHolder();
        settings.Set("NServiceBus.Routing.EndpointName", endpointName);
        return settings;
    }

    // Mirrors how ManifestOutput.Defaults builds the manifest.
    static ManifestOutput.PersistenceManifest ManifestFor(IReadOnlySettings settings) =>
        new() { Prefix = settings.Get<string>("NServiceBus.Routing.EndpointName") };
}
