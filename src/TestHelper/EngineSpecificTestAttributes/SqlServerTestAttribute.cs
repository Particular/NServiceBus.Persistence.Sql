public class SqlServerTestAttribute : EngineSpecificTestAttribute
{
    protected override string ConnectionStringName => "SQLServerConnectionString";
}
