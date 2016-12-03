using System;
using NServiceBus.Persistence;
using NServiceBus.Settings;

static class ReadOnlySettingsExtendions
{
    internal static TValue GetValue<TValue, TStorageType>(this ReadOnlySettings settings, string suffix, Func<TValue> defaultValue)
        where TStorageType : StorageType
    {
        var key = $"SqlPersistence.{typeof(TStorageType).Name}.{suffix}";
        TValue value;
        if (settings.TryGet(key, out value))
        {
            return value;
        }
        if (settings.TryGet($"SqlPersistence.{suffix}", out value))
        {
            return value;
        }
        return defaultValue();
    }
}