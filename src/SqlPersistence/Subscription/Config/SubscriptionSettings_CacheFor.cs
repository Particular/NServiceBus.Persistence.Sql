using NServiceBus.Settings;
using System;

namespace NServiceBus.Persistence.Sql
{

    public partial class SubscriptionSettings
    {

        public void CacheFor(TimeSpan timeSpan)
        {
            Guard.AgainstNegativeAndZero(nameof(timeSpan), timeSpan);
            settings.Set("SqlPersistence.Subscription.CacheFor", timeSpan);
        }

        internal static TimeSpan? GetCacheFor(ReadOnlySettings settings)
        {
            TimeSpan cache;
            if (settings.TryGet("SqlPersistence.Subscription.CacheFor", out cache))
            {
                return cache;
            }
            return null;
        }
    }
}