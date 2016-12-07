using System;
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

        internal static Func<StringBuilder, JsonWriter> GetWriterCreator(ReadOnlySettings settings)
        {
            return settings.GetOrDefault<Func<StringBuilder, JsonWriter>>("SqlPersistence.Saga.WriterCreator");
        }

    }
}