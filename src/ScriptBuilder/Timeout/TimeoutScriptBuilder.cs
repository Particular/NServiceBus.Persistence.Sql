namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System.IO;
    using System.Text;

    public static class TimeoutScriptBuilder
    {
        public static void BuildCreateScript(TextWriter writer, BuildSqlDialect sqlDialect)
        {
            writer.Write(ResourceReader.ReadResource(sqlDialect, "Timeout.Create"));
        }

        public static string BuildCreateScript(BuildSqlDialect sqlDialect)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildCreateScript(stringWriter, sqlDialect);
            }
            return stringBuilder.ToString();
        }

        public static void BuildDropScript(TextWriter writer, BuildSqlDialect sqlDialect)
        {
            writer.Write(ResourceReader.ReadResource(sqlDialect, "Timeout.Drop"));
        }

        public static string BuildDropScript(BuildSqlDialect sqlDialect)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildDropScript(stringWriter, sqlDialect);
            }
            return stringBuilder.ToString();
        }
    }
}