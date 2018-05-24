using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class SqlSagaInstallerFeature : Feature
{
    public SqlSagaInstallerFeature()
    {
        Defaults(s => s.SetDefault<SqlSagaInstallerSettings>(new SqlSagaInstallerSettings()));
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings.Get<SqlSagaInstallerSettings>();
        if (settings.Disabled)
        {
            return;
        }

        settings.ConnectionBuilder = context.Settings.GetConnectionBuilder<StorageType.Sagas>();

        settings.Dialect = context.Settings.GetSqlDialect();
        settings.ScriptDirectory = ScriptLocation.FindScriptDirectory(context.Settings);
        settings.TablePrefix = context.Settings.GetTablePrefix();

        settings.Dialect.ValidateTablePrefix(settings.TablePrefix);
    }
}