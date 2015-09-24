using System;
using NServiceBus.Saga;

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
            return declaringType.FullName;
        }
        return type.FullName.Replace("+", "_");
    }
}