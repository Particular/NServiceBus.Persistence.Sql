using NServiceBus.Settings;

static class SagaConfigExtentions
{
    public static void SetSagaIsEnabled(this SettingsHolder settingsHolder)
    {
        settingsHolder.Set("SqlPersistence.IsEnabledForSaga", true);
    }

    internal static bool IsSagaEnabled(this ReadOnlySettings settings)
    {
        return settings.GetOrDefault<bool>("SqlPersistence.IsEnabledForSaga");
    }
}