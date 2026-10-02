namespace NServiceBus.Persistence.Sql.Analyzer;

#if FIXES
static class DiagnosticIds
#else
public static class DiagnosticIds
#endif
{
    public const string CorrelationPropertyNotFound = "NSBSQLP0001";
}