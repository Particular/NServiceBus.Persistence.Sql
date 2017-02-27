using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NServiceBus.Settings;

namespace NServiceBus.Persistence.Sql
{
    public partial class SagaSettings
    {

        public void WriterCreator(Func<StringBuilder, JsonWriter> writerCreator)
        {
            settings.Set("SqlPersistence.Saga.WriterCreator", writerCreator);
        }

        internal static Func<TextWriter, JsonWriter> GetWriterCreator(ReadOnlySettings settings)
        {
            return settings.GetOrDefault<Func<TextWriter, JsonWriter>>("SqlPersistence.Saga.WriterCreator");
        }

    }
}