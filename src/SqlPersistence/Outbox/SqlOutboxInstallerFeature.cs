using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class SqlOutboxInstallerFeature : Feature
{
    public SqlOutboxInstallerFeature()
    {
        Defaults(s => s.SetDefault<SqlOutboxInstallerSettings>(new SqlOutboxInstallerSettings()));
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings.Get<SqlOutboxInstallerSettings>();
        if (settings.Disabled)
        {
            return;
        }

        settings.ConnectionBuilder = context.Settings.GetConnectionBuilder<StorageType.Outbox>();

        settings.Dialect = context.Settings.GetSqlDialect();
        settings.ScriptDirectory = ScriptLocation.FindScriptDirectory(context.Settings);
        settings.TablePrefix = context.Settings.GetTablePrefix();

        settings.Dialect.ValidateTablePrefix(settings.TablePrefix);
    }
}