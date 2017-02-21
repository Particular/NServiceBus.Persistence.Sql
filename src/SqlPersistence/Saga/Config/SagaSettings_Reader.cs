using System;
using System.IO;
using Newtonsoft.Json;
using NServiceBus.Settings;

namespace NServiceBus.Persistence.Sql
{
    public partial class SagaSettings
    {

        public void ReaderCreator(Func<TextReader, JsonReader> readerCreator)
        {
            settings.Set("SqlPersistence.Saga.ReaderCreator", readerCreator);
        }

        internal static Func<TextReader, JsonReader> GetReaderCreator(ReadOnlySettings settings)
        {
            return settings.GetOrDefault<Func<TextReader, JsonReader>>("SqlPersistence.Saga.ReaderCreator");
        }
    }
}