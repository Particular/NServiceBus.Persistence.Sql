namespace NServiceBus.Persistence.Sql
{
    using Settings;

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