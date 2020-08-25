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
            // ReSharper disable RedundantTypeArgumentsOfMethod
            settings.Set<RetrieveVersionSpecificJsonSettings>(builder);
            // ReSharper restore RedundantTypeArgumentsOfMethod
        }

        internal static RetrieveVersionSpecificJsonSettings GetVersionSettings(ReadOnlySettings settings)
        {
            return settings.GetOrDefault<RetrieveVersionSpecificJsonSettings>();
        }
    }
}