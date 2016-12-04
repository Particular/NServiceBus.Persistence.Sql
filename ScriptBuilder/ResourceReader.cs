using NServiceBus.Persistence.Sql;
using Resourcer;

static class ResourceReader
{
    public static string ReadResource(SqlVarient sqlVarient, string prefix)
    {
        return Resource.AsStringUnChecked($"NServiceBus.Persistence.Sql.ScriptBuilder.{prefix}_{sqlVarient}.sql");
    }
}