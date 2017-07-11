using System;
using System.Collections.Generic;
using NServiceBus.Persistence;

namespace NServiceBus
{
    class EnabledStorageFeatures
    {
        HashSet<Type> enabledFeatures = new HashSet<Type>();

        public void Enable<TStorageType>()
            where TStorageType : StorageType
        {
            enabledFeatures.Add(typeof(TStorageType));
        }

        public bool IsEnabled<TStorageType>()
            where TStorageType : StorageType
        {
            return enabledFeatures.Contains(typeof(TStorageType));
        }
    }


}