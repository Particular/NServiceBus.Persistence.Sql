namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using System;
    using Outbox;

    /// <summary>
    /// Contains extensions methods which allow to configure SQL persistence specific outbox configuration
    /// </summary>
    public static class SqlPersistenceOutboxSettingsExtensions
    {
        /// <summary>
        /// Sets the time to keep the deduplication data to the specified time span.
        /// </summary>
        /// <param name="configuration">The configuration being extended.</param>
        /// <param name="timeToKeepDeduplicationData">The time to keep the deduplication data.
        /// The cleanup process removes entries older than the specified time to keep deduplication data. The time span cannot be negative or zero.</param>
        /// <returns>The configuration</returns>
        public static void SetTimeToKeepDeduplicationData(this OutboxSettings configuration, TimeSpan timeToKeepDeduplicationData)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNegativeAndZero(nameof(timeToKeepDeduplicationData), timeToKeepDeduplicationData);

            configuration.GetSettings().Set(SqlOutboxFeature.TimeToKeepDeduplicationData, timeToKeepDeduplicationData);
        }

        /// <summary>
        /// Sets the frequency to run the deduplication data cleanup task.
        /// </summary>
        /// <param name="configuration">The configuration being extended.</param>
        /// <param name="frequencyToRunDeduplicationDataCleanup">The frequency to run the deduplication data cleanup task. The time span cannot be negative or sero.</param>
        /// <returns>The configuration</returns>
        public static void SetFrequencyToRunDeduplicationDataCleanup(this OutboxSettings configuration, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNegativeAndZero(nameof(frequencyToRunDeduplicationDataCleanup), frequencyToRunDeduplicationDataCleanup);

            configuration.GetSettings().Set(SqlOutboxFeature.FrequencyToRunDeduplicationDataCleanup, frequencyToRunDeduplicationDataCleanup);
        }

        /// <summary>
        /// Disable the built-in outbox deduplication records cleanup.
        /// </summary>
        public static void DisableCleanup(this OutboxSettings configuration)
        {
            configuration.GetSettings().Set(SqlOutboxFeature.DisableCleanup, true);
        }
    }
}
