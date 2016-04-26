using System;
using System.Threading.Tasks;
using NServiceBus.Persistence.SqlServerXml;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
#if (!DEBUG)
[Explicit]
#endif
public class SagaPersisterTests
{
    static string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    static string endpointName = "Endpoint";
    SagaPersister persister;

    [SetUp]
    public void SetUp()
    {
        SetUpAsync().Await();
    }

    async Task SetUpAsync()
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof(MySagaData))
        };
        await DbBuilder.ReCreate(connectionString, endpointName, sagaDefinition);
        var commandBuilder = new SagaCommandBuilder("dbo", endpointName);
        var infoCache = new SagaInfoCache(null, null, commandBuilder, (serializer, type) => { });
        persister = new SagaPersister(infoCache);
    }

    [Test]
    public async Task Complete()
    {
        var id = Guid.NewGuid();
        var sagaData = new MySagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        };

        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, null, storageSession, null);
            await persister.Complete(sagaData, storageSession, null);
            Assert.IsNull(await persister.Get<MySagaData>(id, storageSession, null));
        }
    }

    [Test]
    public void GetById()
    {
        var id = Guid.NewGuid();
        var result = GetByIdAsync(id).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<MySagaData> GetByIdAsync(Guid id)
    {
        var sagaData = new MySagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        };

        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, null, storageSession, null);
            return await persister.Get<MySagaData>(id, storageSession, null);
        }
    }

    public class MySagaData : XmlSagaData
    {
        public string Property { get; set; }
    }

    [Test]
    public void GetByMapping()
    {
        var id = Guid.NewGuid();
        var result = GetByMappingAsync(id).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<MySagaData> GetByMappingAsync(Guid id)
    {
        var sagaData = new MySagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        };
        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, null, storageSession, null);
            return await persister.Get<MySagaData>("Property", "theProperty", storageSession, null);
        }
    }

    [Test]
    //TODO:
    public async Task SaveDuplicateShouldThrow()
    {
        var sagaData1 = new MySagaData
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        };
        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData1, null, storageSession, null);
            var sagaData2 = new MySagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                Property = "theProperty"
            };
            await persister.Save(sagaData2, null, storageSession, null);
        }
    }
}