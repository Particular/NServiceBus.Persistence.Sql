public class MySqlTestAttribute : EngineSpecificTestAttribute
{
    protected override string ConnectionStringName => "MySQLConnectionString";
}
