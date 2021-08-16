namespace NServiceBus.Persistence.Sql
{
    using Newtonsoft.Json;
    using Settings;

    public partial class SagaSettings
    {
        /// <summary>
        /// The <see cref="JsonSerializerSettings"/> to use for serializing sagas.
        /// </summary>
        public void JsonSettings(JsonSerializerSettings jsonSerializerSettings)
        {
            settings.Set("SqlPersistence.Saga.JsonSerializerSettings", jsonSerializerSettings);
        }

        internal static JsonSerializerSettings GetJsonSerializerSettings(IReadOnlySettings settings)
        {
            return settings.GetOrDefault<JsonSerializerSettings>("SqlPersistence.Saga.JsonSerializerSettings");
        }
    }
}