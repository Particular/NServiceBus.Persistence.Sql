namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using Particular.Obsoletes;
    using Settings;

    public partial class SagaSettings
    {
        /// <summary>
        /// Builds up a <see cref="Newtonsoft.Json.JsonWriter"/> for serializing saga data.
        /// </summary>
        /// <remarks>
        /// The <see cref="TextWriter"/> is owned by the persister and only valid while the saga data is serialized.
        /// Don't keep a reference to it or close it before serialization completes. The persister disposes the returned
        /// <see cref="Newtonsoft.Json.JsonWriter"/>, which may close the <see cref="TextWriter"/>.
        /// When both overloads are configured, this one is used.
        /// </remarks>
        public void WriterCreator(Func<TextWriter, JsonWriter> writerCreator)
        {
            settings.Set("SqlPersistence.Saga.TextWriterCreator", writerCreator);
        }

        /// <summary>
        /// Builds up a <see cref="Newtonsoft.Json.JsonWriter"/> for serializing saga data.
        /// </summary>
        [ObsoleteMetadata(
            Message = "Use the overload that accepts a Func<TextWriter, JsonWriter>, which writes directly into the buffer the persister provides",
            TreatAsErrorFromVersion = "10",
            RemoveInVersion = "11")]
        [Obsolete("Use the overload that accepts a Func<TextWriter, JsonWriter>, which writes directly into the buffer the persister provides. Will be treated as an error from version 10.0.0. Will be removed in version 11.0.0.", false)]
        public void WriterCreator(Func<StringBuilder, JsonWriter> writerCreator)
        {
            settings.Set("SqlPersistence.Saga.WriterCreator", writerCreator);
        }

        internal static Func<TextWriter, JsonWriter> GetTextWriterCreator(IReadOnlySettings settings)
        {
            return settings.GetOrDefault<Func<TextWriter, JsonWriter>>("SqlPersistence.Saga.TextWriterCreator");
        }

        internal static Func<StringBuilder, JsonWriter> GetWriterCreator(IReadOnlySettings settings)
        {
            return settings.GetOrDefault<Func<StringBuilder, JsonWriter>>("SqlPersistence.Saga.WriterCreator");
        }
    }
}
