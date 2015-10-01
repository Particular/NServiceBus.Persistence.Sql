using System;
using System.Collections.Generic;
using System.Linq;

public static class MappedTypeVerifier
{
    static List<string> validTypes;
    static MappedTypeVerifier()
    {
        validTypes = GetValidTypes()
            .Select(x=>x.FullName)
            .ToList();
    }

    static IEnumerable<Type> GetValidTypes()
    {
        yield return typeof (string);
        yield return typeof (char);
        yield return typeof (int);
        yield return typeof (long);
        yield return typeof (short);
        yield return typeof (uint);
        yield return typeof (ulong);
        yield return typeof (ushort);
        yield return typeof (Guid);
        yield return typeof (float);
        yield return typeof (DateTime);
        yield return typeof (DateTimeOffset);
        yield return typeof (TimeSpan);
    }

    public static void Verify(string typeName)
    {
        if (validTypes.Contains(typeName))
        {
            return;
        }
        var message = $"The type {typeName} is not valid to be used as a mapped message or as a unique property. The valid types are {string.Join(", ", validTypes)}";
        throw new Exception(message);
    }
}