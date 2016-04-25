using System.IO;
using Resourcer;

namespace NServiceBus.Persistence.SqlServerXml
{
    public static class TimeoutScriptBuilder
    {

        public static void BuildCreateScript(TextWriter writer)
        {
            writer.Write(Resource.AsString("TimeoutCreate.sql"));
        }

        public static void BuildDropScript(TextWriter writer)
        {
            writer.Write(Resource.AsString("TimeoutDrop.sql"));
        }
    }
}