public class AuroraPostgreSqlTestAttribute : EngineSpecificTestAttribute
{
    protected override string ConnectionStringName => AuroraPostgreSqlConnectionBuilder.EnvVarName;
}