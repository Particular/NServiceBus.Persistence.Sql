namespace NServiceBus.Persistence.Sql
{
    using Settings;

    /// <summary>
    /// Configuration options for Timeout persistence.
    /// </summary>
    public partial class TimeoutSettings
    {
        SettingsHolder settings;

        internal TimeoutSettings(SettingsHolder settings)
        {
            this.settings = settings;
        }
    }
}