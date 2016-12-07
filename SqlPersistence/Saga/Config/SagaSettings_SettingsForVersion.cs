using NServiceBus.Settings;

namespace NServiceBus.Persistence.Sql
{
    public partial class SagaSettings
    {

        public void JsonSettingsForVersion(RetrieveVersionSpecificJsonSettings builder)
        {
            settings.Set<RetrieveVersionSpecificJsonSettings>(builder);
        }

        internal static RetrieveVersionSpecificJsonSettings GetVersionSettings(ReadOnlySettings settings)
        {
            return settings.GetOrDefault<RetrieveVersionSpecificJsonSettings>();
        }

    }
}