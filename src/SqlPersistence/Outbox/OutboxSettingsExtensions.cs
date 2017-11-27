using System;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Outbox;
using NServiceBus.Settings;

/// <summary>
/// Contains InMemoryOutbox-related settings extensions.
/// </summary>
public static class OutboxSettingsExtensions
{
    const string RetentionPeriodKey = "SqlPersistence.TimeToKeepDeduplicationEntries";
    const string IntervalKey = "SqlPersistence.DeduplicationCleanupInterval";
    const string BatchSizeKey = "SqlPersistence.DeduplicationCleanupBatchSize";

    /// <summary>
    /// Specifies how long the outbox should keep message data in storage to be able to deduplicate. Defaults to 7 days.
    /// </summary>
    /// <param name="settings">The outbox settings.</param>
    /// <param name="time">
    /// Defines the <see cref="TimeSpan"/> which indicates how long the outbox deduplication entries should be kept.
    /// For example, if <code>TimeSpan.FromDays(1)</code> is used, the deduplication entries are kept for no longer than one day.
    /// It is not possible to use a negative or zero TimeSpan value.
    /// </param>
    public static OutboxSettings TimeToKeepDeduplicationData(this OutboxSettings settings, TimeSpan time)
    {
        Guard.AgainstNegativeAndZero(nameof(time), time);
        settings.GetSettings().Set(RetentionPeriodKey, time);
        return settings;
    }

    /// <summary>
    /// Specifies how frequent the cleanup of deduplication data should be run. Defaults to 1 minute.
    /// </summary>
    /// <param name="settings">The outbox settings.</param>
    /// <param name="time">Cleanup interval.</param>
    public static OutboxSettings DeduplicationDataCleanupInterval(this OutboxSettings settings, TimeSpan time)
    {
        Guard.AgainstNegativeAndZero(nameof(time), time);
        settings.GetSettings().Set(IntervalKey, time);
        return settings;
    }

    /// <summary>
    /// Specifies maximum size of deduplication data cleanup batch (row limit for a single DELETE call). Defaults to 10000.
    /// </summary>
    /// <param name="settings">The outbox settings.</param>
    /// <param name="batchSize">Batch size.</param>
    public static OutboxSettings DeduplicationDataCleanupBatchSize(this OutboxSettings settings, int batchSize)
    {
        Guard.AgainstNegativeAndZero(nameof(batchSize), batchSize);
        settings.GetSettings().Set(BatchSizeKey, batchSize);
        return settings;
    }

    internal static TimeSpan GetTimeToKeepDeduplicationData(this ReadOnlySettings settings)
    {
        return settings.HasSetting(RetentionPeriodKey) 
            ? settings.Get<TimeSpan>(RetentionPeriodKey) 
            : TimeSpan.FromDays(7);
    }

    internal static TimeSpan GetDeduplicationDataCleanupInterval(this ReadOnlySettings settings)
    {
        return settings.HasSetting(IntervalKey)
            ? settings.Get<TimeSpan>(IntervalKey)
            : TimeSpan.FromMinutes(1);
    }

    internal static int GetDeduplicationDataCleanupBatchSize(this ReadOnlySettings settings)
    {
        return settings.HasSetting(BatchSizeKey)
            ? settings.Get<int>(BatchSizeKey)
            : 10000;
    }
}