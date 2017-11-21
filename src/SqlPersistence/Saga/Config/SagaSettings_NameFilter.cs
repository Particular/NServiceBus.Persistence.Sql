namespace NServiceBus.Persistence.Sql
{
    using Settings;
    using System;

    public partial class SagaSettings
    {
        internal void NameFilter(Func<string, string> nameFilter)
        {
            settings.Set("SqlPersistence.Saga.NameFilter", nameFilter);
        }

        internal static Func<string, string> GetNameFilter(ReadOnlySettings settings)
        {
            return settings.GetOrDefault<Func<string, string>>("SqlPersistence.Saga.NameFilter");
        }
    }
}