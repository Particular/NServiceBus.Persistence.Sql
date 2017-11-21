#pragma warning disable CS0419
namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using Settings;

    public partial class SagaSettings
    {

        /// <summary>
        /// Builds up a <see cref="Newtonsoft.Json.JsonWriter"/> for serializing saga data.
        /// </summary>
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
#pragma warning restore CS0419