using System.IO;
using System.Text;

namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    public static class OutboxScriptBuilder
    {

        public static void BuildCreateScript(TextWriter writer, SqlVarient sqlVarient)
        {
            writer.Write(ResourceReader.ReadResource(sqlVarient, "Outbox.Create"));
        }

        public static string BuildCreateScript(SqlVarient sqlVarient)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildCreateScript(stringWriter, sqlVarient);
            }
            return stringBuilder.ToString();
        }

        public static void BuildDropScript(TextWriter writer, SqlVarient sqlVarient)
        {
            writer.Write(ResourceReader.ReadResource(sqlVarient, "Outbox.Drop"));
        }

        public static string BuildDropScript(SqlVarient sqlVarient)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildDropScript(stringWriter, sqlVarient);
            }
            return stringBuilder.ToString();
        }
    }
}