
namespace NServiceBus
{
    using System.Transactions;
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
        public static void KeepDeduplicationDataFor(this OutboxSettings configuration, TimeSpan timeToKeepDeduplicationData)
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
        public static void RunDeduplicationDataCleanupEvery(this OutboxSettings configuration, TimeSpan frequencyToRunDeduplicationDataCleanup)
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
            Guard.AgainstNull(nameof(configuration), configuration);

            configuration.GetSettings().Set(SqlOutboxFeature.DisableCleanup, true);
        }

        /// <summary>
        /// Enables outbox pessimistic mode in which Outbox record is created prior to invoking the message handler. As a result,
        /// the likelihood of invoking the message handler multiple times in case of duplicate messages is much lower.
        ///
        /// Note that the outbox always ensures that the transactional side effects of message processing are applied once. The pessimistic
        /// mode only affects non-transactional side effects. In the pessimistic mode the latter are less likely to be applied
        /// multiple times but that can still happen e.g. when a message processing attempt is interrupted.
        /// </summary>
        public static void UsePessimisticConcurrencyControl(this OutboxSettings outboxSettings)
        {
            outboxSettings.GetSettings().Set(SqlOutboxFeature.ConcurrencyMode, true);
        }

        /// <summary>
        /// Configures the outbox to use specific transaction level.
        /// Only levels Read Committed, Repeatable Read and Serializable are supported.
        /// </summary>
        public static void TransactionIsolationLevel(this OutboxSettings outboxSettings, System.Data.IsolationLevel isolationLevel)
        {
            if (isolationLevel == System.Data.IsolationLevel.Chaos
                || isolationLevel == System.Data.IsolationLevel.ReadUncommitted
                || isolationLevel == System.Data.IsolationLevel.Snapshot
                || isolationLevel == System.Data.IsolationLevel.Unspecified)
            {
                throw new Exception($"Isolation level {isolationLevel} is not supported.");
            }
            outboxSettings.GetSettings().Set(SqlOutboxFeature.AdoTransactionIsolationLevel, isolationLevel);
        }

        /// <summary>
        /// Configures the outbox to use TransactionScope instead of SqlTransaction. This allows wrapping the
        /// the outbox transaction (and synchronized storage session it manages) and other database transactions in a single scope - provided that
        /// Distributed Transaction Coordinator (DTC) infrastructure is configured.
        ///
        /// Uses the default isolation level (Serializable).
        /// </summary>
        public static void UseTransactionScope(this OutboxSettings outboxSettings)
        {
            UseTransactionScope(outboxSettings, IsolationLevel.Serializable);
        }

        /// <summary>
        /// Configures the outbox to use TransactionScope instead of SqlTransaction. This allows wrapping the
        /// the outbox transaction (and synchronized storage session it manages) and other database transactions in a single scope - provided that
        /// Distributed Transaction Coordinator (DTC) infrastructure is configured.
        /// </summary>
        /// <param name="outboxSettings">Outbox settings.</param>
        /// <param name="isolationLevel">Isolation level to use. Only levels Read Committed, Repeatable Read and Serializable are supported.</param>
        public static void UseTransactionScope(this OutboxSettings outboxSettings, IsolationLevel isolationLevel)
        {
            if (isolationLevel == IsolationLevel.Chaos
                || isolationLevel == IsolationLevel.ReadUncommitted
                || isolationLevel == IsolationLevel.Snapshot
                || isolationLevel == IsolationLevel.Unspecified)
            {
                throw new Exception($"Isolation level {isolationLevel} is not supported.");
            }
            outboxSettings.GetSettings().Set(SqlOutboxFeature.UseTransactionScope, true);
            outboxSettings.GetSettings().Set(SqlOutboxFeature.TransactionScopeIsolationLevel, isolationLevel);
        }
    }
}
