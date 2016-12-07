using NServiceBus.Settings;

namespace NServiceBus.Persistence.Sql
{
    public partial class SagaSettings
    {

        SettingsHolder settings;

        internal SagaSettings(SettingsHolder settings)
        {
            this.settings = settings;
        }

    }
}