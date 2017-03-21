using NServiceBus.Settings;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Configuration options for Subscription persistence.
    /// </summary>
    public partial class SubscriptionSettings
    {

        SettingsHolder settings;

        internal SubscriptionSettings(SettingsHolder settings)
        {
            this.settings = settings;
        }

    }
}