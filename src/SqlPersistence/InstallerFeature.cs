using NServiceBus;
using NServiceBus.Features;

class InstallerFeature : Feature
{
    public InstallerFeature()
    {
        Defaults(s => s.SetDefault<InstallerSettings>(new InstallerSettings()));
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings.Get<InstallerSettings>();
        if (settings.Disabled)
        {
            return;
        }

        settings.ConnectionBuilder = context.Settings.GetConnectionBuilder();
        settings.Dialect = context.Settings.GetSqlDialect();
        settings.ScriptDirectory = ScriptLocation.FindScriptDirectory(context.Settings);
        settings.TablePrefix = context.Settings.GetTablePrefix();

        settings.Dialect.ValidateTablePrefix(settings.TablePrefix);
    }
}