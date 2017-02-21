using System.IO;
using System.Text;

namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    public static class OutboxScriptBuilder
    {

        public static void BuildCreateScript(TextWriter writer, BuildSqlVariant sqlVariant)
        {
            writer.Write(ResourceReader.ReadResource(sqlVariant, "Outbox.Create"));
        }

        public static string BuildCreateScript(BuildSqlVariant sqlVariant)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildCreateScript(stringWriter, sqlVariant);
            }
            return stringBuilder.ToString();
        }

        public static void BuildDropScript(TextWriter writer, BuildSqlVariant sqlVariant)
        {
            writer.Write(ResourceReader.ReadResource(sqlVariant, "Outbox.Drop"));
        }

        public static string BuildDropScript(BuildSqlVariant sqlVariant)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildDropScript(stringWriter, sqlVariant);
            }
            return stringBuilder.ToString();
        }
    }
}