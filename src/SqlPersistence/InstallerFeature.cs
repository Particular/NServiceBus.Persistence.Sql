using NServiceBus;
using NServiceBus.Features;

class InstallerFeature : Feature
{
    public InstallerFeature()
    {
        Defaults(s => s.SetDefault(new InstallerSettings()));
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings.Get<InstallerSettings>();
        if (settings.Disabled)
        {
            return;
        }

        settings.ConnectionBuilder = storageType => context.Settings.GetConnectionBuilder(storageType).BuildNonContextual();
        settings.Dialect = context.Settings.GetSqlDialect();
        settings.ScriptDirectory = ScriptLocation.FindScriptDirectory(context.Settings);
        settings.TablePrefix = context.Settings.GetTablePrefix();
        settings.IsMultiTenant = context.Settings.EndpointIsMultiTenant();

        settings.Dialect.ValidateTablePrefix(settings.TablePrefix);
    }
}