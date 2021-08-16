namespace NServiceBus.Persistence.Sql
{
    using Settings;

    public partial class SagaSettings
    {
        /// <summary>
        /// Configure how a specific saga data version will be serialized.
        /// </summary>
        public void JsonSettingsForVersion(RetrieveVersionSpecificJsonSettings builder)
        {
            settings.Set(builder);
        }

        internal static RetrieveVersionSpecificJsonSettings GetVersionSettings(IReadOnlySettings settings)
        {
            return settings.GetOrDefault<RetrieveVersionSpecificJsonSettings>();
        }
    }
}