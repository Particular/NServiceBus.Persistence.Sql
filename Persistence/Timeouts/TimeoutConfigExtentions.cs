using NServiceBus.Settings;

static class TimeoutConfigExtentions
{
    public static void SetTimeoutIsEnabled(this SettingsHolder settingsHolder)
    {
        settingsHolder.Set("SqlPersistence.IsEnabledForTimeout", true);
    }

    internal static bool IsTimeoutEnabled(this ReadOnlySettings settings)
    {
        return settings.GetOrDefault<bool>("SqlPersistence.IsEnabledForTimeout");
    }
}