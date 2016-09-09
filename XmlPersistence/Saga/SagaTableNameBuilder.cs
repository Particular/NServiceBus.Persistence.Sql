using System;
using NServiceBus;

static class SagaTableNameBuilder
{
    public static string GetTableSuffix(Type type)
    {
        var declaringType = type.DeclaringType;
        if (declaringType == null)
        {
            return type.Name;
        }
        if (typeof(Saga).IsAssignableFrom(declaringType))
        {
            return declaringType.Name.Replace("+", "_");
        }
        return type.Name.Replace("+", "_");
    }
}