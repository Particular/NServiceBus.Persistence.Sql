using NServiceBus.Settings;

static class SubscriptionConfigExtentions
{
    public static void SetSubscriptionIsEnabled(this SettingsHolder settingsHolder)
    {
        settingsHolder.Set("SqlPersistence.IsEnabledForSubscription", true);
    }

    internal static bool IsSubscriptionEnabled(this ReadOnlySettings settings)
    {
        return settings.GetOrDefault<bool>("SqlPersistence.IsEnabledForSubscription");
    }
}