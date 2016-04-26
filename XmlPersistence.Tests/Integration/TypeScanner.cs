using System;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus.Persistence.SqlServerXml;

public static class TypeScanner
{
    public static IEnumerable<Type> NestedTypes<T>(params Type[] extraTypes)
    {
        var rootType = typeof(T);
        yield return rootType;
        var flags = BindingFlags.NonPublic | BindingFlags.Public;
        foreach (var nestedType in rootType.GetNestedTypes(flags))
        {
            yield return nestedType;
        }

        var persistenceTypes = typeof(SqlXmlPersistence).Assembly.GetTypes();
        foreach (var extraType in persistenceTypes)
        {
            yield return extraType;
        }
        foreach (var extraType in extraTypes)
        {
            yield return extraType;
        }
    }
}