using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;
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
    public async Task SetUp()
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof(MySaga)),
            CorrelationMember = new CorrelationMember
            {
                Name = "CorrelationProperty",
                Type = CorrelationMemberType.String
            },
            TransitionalCorrelationMember = new CorrelationMember
            {
                Name = "TransitionalCorrelationProperty",
                Type = CorrelationMemberType.String
            }
        };
        await DbBuilder.ReCreate(connectionString, endpointName, sagaDefinition);
        var commandBuilder = new SagaCommandBuilder("dbo", endpointName + ".");
        var infoCache = new SagaInfoCache(null, null, commandBuilder);
        persister = new SagaPersister(infoCache);
    }

    [Test]
    public async Task Complete()
    {
        var id = Guid.NewGuid();
        var sagaData = new MySaga.MySagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theProperty"
        };

        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, storageSession,typeof(MySaga));
            await persister.Complete(sagaData, storageSession, typeof(MySaga));
            Assert.IsNull(await persister.Get<MySaga.MySagaData>(id, storageSession, typeof(MySaga)));
        }
    }


    [Test]
    public void GetById()
    {
        var id = Guid.NewGuid();
        var result = GetByIdAsync(id).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<MySaga.MySagaData> GetByIdAsync(Guid id)
    {
        var sagaData = new MySaga.MySagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "theSimpleProperty",
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty"
        };

        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, storageSession, typeof(MySaga));
            return await persister.Get<MySaga.MySagaData>(id, storageSession, typeof(MySaga));
        }
    }

    [SqlSaga(
        correlationId:nameof(MySagaData.CorrelationProperty),
        transitionalCorrelationId:nameof(MySagaData.TransitionalCorrelationProperty)
        )]
    public class MySaga : Saga<MySaga.MySagaData>
    {
        public class MySagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
            public string TransitionalCorrelationProperty { get; set; }
            public string SimpleProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
        {
        }
    }

    [Test]
    public void GetByMapping()
    {
        var id = Guid.NewGuid();
        var result = GetByMappingAsync(id).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<MySaga.MySagaData> GetByMappingAsync(Guid id)
    {
        var sagaData = new MySaga.MySagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty",
            SimpleProperty = "theSimpleProperty"
        };
        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, storageSession, typeof(MySaga));
            return await persister.Get<MySaga.MySagaData>("CorrelationProperty", "theCorrelationProperty", storageSession, typeof(MySaga));
        }
    }

    [Test]
    //TODO:
    public async Task SaveDuplicateShouldThrow()
    {
        var sagaData1 = new MySaga.MySagaData
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty",
            SimpleProperty = "theSimpleProperty"
        };
        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData1, storageSession, typeof(MySaga));
            var sagaData2 = new MySaga.MySagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                CorrelationProperty = "theCorrelationProperty",
                TransitionalCorrelationProperty = "theTransitionalCorrelationProperty",
                SimpleProperty = "theSimpleProperty"
            };
            await persister.Save(sagaData2, storageSession, typeof(MySaga));
            //var throwsAsync = Assert.ThrowsAsync<Exception>(() => persister.Save(sagaData2, null, storageSession, null));
            //Assert.IsTrue(throwsAsync.InnerException.Message.Contains("Violation of UNIQUE KEY constraint"));
            await storageSession.CompleteAsync();
        }
    }
}