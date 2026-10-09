using System;
using System.IO.Hashing;
using System.Text;

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

    // Unique per endpoint so fixtures running in parallel never share a saga table. The hash covers the full saga name because the readable part gets cut.
    public static Func<string, string> ForEndpoint(string assemblyPrefix, string endpointName) =>
        sagaName =>
        {
            var hash = Convert.ToHexString(XxHash32.Hash(Encoding.UTF8.GetBytes($"{endpointName}/{sagaName}")));
            return Create(assemblyPrefix, $"{hash}_{sagaName}");
        };
}
