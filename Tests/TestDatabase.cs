using System;

class TestDatabase : IDisposable
{
    string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    string endpointName = "Endpoint";
    public TestDatabase()
    {
        TimeoutInstaller.Drop(endpointName, connectionString);
        TimeoutInstaller.Install(endpointName, connectionString);
        TimeoutPersister = new TimeoutPersister(connectionString, "dbo", endpointName);
    }

    public TimeoutPersister TimeoutPersister;

    public void Dispose()
    {
      //  TimeoutInstaller.Drop(endpointName, connectionString);
    }
}