using System;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus;

static class EndpointConfigurationExtensions
{
    public static void SetTypesToScan(this EndpointConfiguration endpointConfiguration, IEnumerable<Type> typesToScan)
    {
        var methodInfo = typeof(EndpointConfiguration).GetMethod("TypesToScanInternal", BindingFlags.NonPublic | BindingFlags.Instance);
        methodInfo.Invoke(endpointConfiguration, new object[]
        {
            typesToScan
        });
    }
}