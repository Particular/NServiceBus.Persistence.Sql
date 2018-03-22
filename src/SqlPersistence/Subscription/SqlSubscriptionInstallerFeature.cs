using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class SqlSubscriptionInstallerFeature : Feature
{
    public SqlSubscriptionInstallerFeature()
    {
        Defaults(s => s.SetDefault<SqlSubscriptionInstallerSettings>(new SqlSubscriptionInstallerSettings()));
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings.Get<SqlSubscriptionInstallerSettings>();
        if (settings.Disabled)
        {
            return;
        }

        settings.ConnectionBuilder = context.Settings.GetConnectionBuilder<StorageType.Subscriptions>();

        settings.Dialect = context.Settings.GetSqlDialect();
        settings.ScriptDirectory = ScriptLocation.FindScriptDirectory(context.Settings);
        settings.TablePrefix = context.Settings.GetTablePrefix();

        settings.Dialect.ValidateTablePrefix(settings.TablePrefix);
    }
}