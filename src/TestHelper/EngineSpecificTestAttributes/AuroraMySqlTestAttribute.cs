public class AuroraMySqlTestAttribute : EngineSpecificTestAttribute
{
    protected override string ConnectionStringName => AuroraMySqlConnectionBuilder.EnvVarName;
}
