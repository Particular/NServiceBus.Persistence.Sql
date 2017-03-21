using Newtonsoft.Json;
using NServiceBus.Settings;

namespace NServiceBus.Persistence.Sql
{

    public partial class SagaSettings
    {
        /// <summary>
        /// The <see cref="JsonSerializerSettings"/> to use for serializing sagas.
        /// </summary>
        public void JsonSettings(JsonSerializerSettings jsonSerializerSettings)
        {
            settings.Set("SqlPersistence.Saga.JsonSerializerSettings", jsonSerializerSettings);
        }

        internal static JsonSerializerSettings GetJsonSerializerSettings(ReadOnlySettings settings)
        {
            return settings.GetOrDefault<JsonSerializerSettings>("SqlPersistence.Saga.JsonSerializerSettings");
        }
    }
}