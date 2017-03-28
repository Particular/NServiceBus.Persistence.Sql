using NServiceBus.Settings;
using System;

namespace NServiceBus.Persistence.Sql
{

    public partial class SubscriptionSettings
    {

        /// <summary>
        /// Cache subscriptions for a given <see cref="TimeSpan"/>.
        /// </summary>
        public void CacheFor(TimeSpan timeSpan)
        {
            Guard.AgainstNegativeAndZero(nameof(timeSpan), timeSpan);
            settings.Set("SqlPersistence.Subscription.CacheFor", timeSpan);
        }

        /// <summary>
        /// Do not cache subscriptions.
        /// </summary>
        public void DisableCache()
        {
            settings.Set("SqlPersistence.Subscription.CacheFor", TimeSpan.Zero);
        }

        internal static TimeSpan? GetCacheFor(ReadOnlySettings settings)
        {
            TimeSpan cache;
            if (settings.TryGet("SqlPersistence.Subscription.CacheFor", out cache))
            {
                // since ReadOnlySettings.TryGet will return false if the underlying value is null
                // we use TimeSpan.Zero as a marker for DisableCache
                if (cache == TimeSpan.Zero)
                {
                    return null;
                }
                return cache;
            }
            throw new Exception(@"Subscription caching is a required settings. Access this setting using the following:
var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
var subscriptions = persistence.SubscriptionSettings();
subscriptions.CacheFor(TimeSpan.FromMinutes(1));
// OR
subscriptions.DisableCache();
");
        }
    }
}