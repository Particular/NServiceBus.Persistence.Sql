namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System.IO;
    using System.Text;

    public static class OutboxScriptBuilder
    {
        public static void BuildCreateScript(TextWriter writer, BuildSqlDialect sqlDialect)
        {
            var resource = ResourceReader.ReadResource(sqlDialect, "Outbox.Create");
            resource = resource.ReplaceLineEndings();

            writer.Write(resource);
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
            var resource = ResourceReader.ReadResource(sqlDialect, "Outbox.Drop");
            resource = resource.ReplaceLineEndings();

            writer.Write(resource);
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