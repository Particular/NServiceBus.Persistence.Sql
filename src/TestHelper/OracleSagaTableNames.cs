using System;

// Oracle saga tables ignore the table prefix, so assemblies that share a schema get distinct saga table names instead.
public static class OracleSagaTableNames
{
    public const string AcceptanceTestsPrefix = "At_";
    public const string PersistenceTestsPrefix = "Pt_";

    const int MaxLength = 27;

    public static string Create(string assemblyPrefix, string tableSuffix)
    {
        var name = assemblyPrefix + tableSuffix;
        return name[..Math.Min(MaxLength, name.Length)];
    }
}
