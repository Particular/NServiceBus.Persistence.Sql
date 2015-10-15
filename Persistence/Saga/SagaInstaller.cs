using System.IO;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Persistence;

class SagaInstaller : INeedToInstallSomething
{

    public void Install(string identity, Configure config)
    {
        var settings = config.Settings;
        if (!settings.GetFeatureEnabled<StorageType.Sagas>())
        {
            return;
        }
        if (settings.GetDisableInstaller<StorageType.Sagas>())
        {
            return;
        }
        var connectionString = settings.GetConnectionString<StorageType.Sagas>();
        var endpointName = config.Settings.EndpointName();
        var sagasDirectory = Path.Combine(ScriptLocation.FindScriptDirectory(), "Sagas");
        var sagaScripts = Directory.EnumerateFiles(sagasDirectory, "*_Create.sql");

        SqlHelpers.Execute(connectionString, sagaScripts, collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Sagas>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }
}