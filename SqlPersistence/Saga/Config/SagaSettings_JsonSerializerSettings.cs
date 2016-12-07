using Newtonsoft.Json;
using NServiceBus.Settings;

namespace NServiceBus.Persistence.Sql
{

    public partial class SagaSettings
    {

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