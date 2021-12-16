public class PostgreSqlTestAttribute : EngineSpecificTestAttribute
{
    protected override string ConnectionStringName => "PostgreSqlConnectionString";
}
