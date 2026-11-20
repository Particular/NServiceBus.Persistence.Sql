namespace NServiceBus.Persistence.Sql
{
    using System;
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

        internal static Func<StringBuilder, JsonWriter> GetWriterCreator(IReadOnlySettings settings)
        {
            return settings.GetOrDefault<Func<StringBuilder, JsonWriter>>("SqlPersistence.Saga.WriterCreator");
        }

    }
}