using NServiceBus.Settings;

namespace NServiceBus.Persistence.Sql
{
    public partial class SubscriptionSettings
    {

        SettingsHolder settings;

        internal SubscriptionSettings(SettingsHolder settings)
        {
            this.settings = settings;
        }

    }
}