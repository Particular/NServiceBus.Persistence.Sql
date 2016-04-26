using System;
using NServiceBus;

static class SagaTableNameBuilder
{
    public static string GetTableSuffix(Type type)
    {
        var declaringType = type.DeclaringType;
        if (declaringType == null)
        {
            return type.FullName;
        }
        if (typeof(Saga).IsAssignableFrom(declaringType))
        {
            return declaringType.FullName.Replace("+", "_");
        }
        return type.FullName.Replace("+", "_");
    }
}