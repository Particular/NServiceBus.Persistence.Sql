using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class SqlTimeoutInstallerFeature : Feature
{
    public SqlTimeoutInstallerFeature()
    {
        Defaults(s => s.SetDefault<SqlTimeoutInstallerSettings>(new SqlTimeoutInstallerSettings()));
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings.Get<SqlTimeoutInstallerSettings>();
        if (settings.Disabled)
        {
            return;
        }

        settings.ConnectionBuilder = context.Settings.GetConnectionBuilder<StorageType.Timeouts>();

        settings.Dialect = context.Settings.GetSqlDialect();
        settings.ScriptDirectory = ScriptLocation.FindScriptDirectory(context.Settings);
        settings.TablePrefix = context.Settings.GetTablePrefix();

        settings.Dialect.ValidateTablePrefix(settings.TablePrefix);
    }
}