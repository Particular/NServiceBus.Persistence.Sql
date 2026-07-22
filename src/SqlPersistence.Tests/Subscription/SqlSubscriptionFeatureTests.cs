using NServiceBus;
using NServiceBus.Settings;
using NUnit.Framework;

[TestFixture]
public class SqlSubscriptionFeatureTests
{
    [Test]
    public void Subscription_manifest_prefix_cleans_endpoint_name()
    {
        var settings = SettingsFor("My.Endpoint");
        var manifest = ManifestFor(settings);

        SqlSubscriptionFeature.ConfigureSubscriptionManifest(manifest, settings, new SqlDialect.MsSqlServer());

        Assert.That(manifest.SqlSubscriptions.TableName, Is.EqualTo("[dbo].[My_Endpoint_SubscriptionData]"));
    }

    [Test]
    public void Subscription_manifest_prefix_honours_custom_table_prefix()
    {
        var settings = SettingsFor("My.Endpoint");
        settings.Set("SqlPersistence.TablePrefix", "Foo_");
        var manifest = ManifestFor(settings);

        SqlSubscriptionFeature.ConfigureSubscriptionManifest(manifest, settings, new SqlDialect.MsSqlServer());

        Assert.That(manifest.SqlSubscriptions.TableName, Is.EqualTo("[dbo].[Foo_SubscriptionData]"));
    }

    [Test]
    public void Subscription_manifest_table_name_matches_runtime_table_name()
    {
        var settings = SettingsFor("My.Endpoint");
        var dialect = new SqlDialect.MsSqlServer();
        var manifest = ManifestFor(settings);

        SqlSubscriptionFeature.ConfigureSubscriptionManifest(manifest, settings, dialect);

        var runtime = dialect.GetSubscriptionTableName(settings.GetTablePrefix(settings.EndpointName()));

        Assert.That(manifest.SqlSubscriptions.TableName, Is.EqualTo(runtime));
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
