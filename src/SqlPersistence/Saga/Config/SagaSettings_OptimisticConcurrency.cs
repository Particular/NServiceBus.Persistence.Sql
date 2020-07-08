namespace NServiceBus.Persistence.Sql
{
    using Newtonsoft.Json;
    using Settings;

    public partial class SagaSettings
    {
        /// <summary>
        /// The <see cref="JsonSerializerSettings"/> to use for serializing sagas.
        /// </summary>
        public void UseOptimisticConcurrency()
        {
            settings.Set("SqlPersistence.Saga.UseOptimisticConcurrency", true);
        }

        internal static bool GetUsesOptimisticConcurrency(ReadOnlySettings settings)
        {
            return !settings.HasSetting("SqlPersistence.Saga.UseOptimisticConcurrency") || settings.Get<bool>("SqlPersistence.Saga.UseOptimisticConcurrency");
        }
    }
}