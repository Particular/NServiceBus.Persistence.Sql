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
    [Test]
    public async Task Complete()
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof (MySagaData))
        };
        using (var testDatabase = new SagaDatabase(sagaDefinition))
        {
            var persister = testDatabase.Persister;
            var id = Guid.NewGuid();
            var sagaData = new MySagaData
            {
                Id = id,
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                Property = "theProperty"
            };
            var storageSession = new StorageSession(testDatabase.SqlConnection, testDatabase.SqlTransaction,true);
            await persister.Save(sagaData, null, storageSession, null);
            await persister.Complete(sagaData, storageSession, null);
            Assert.IsNull(await persister.Get<MySagaData>(id, storageSession, null));
        }
    }

    [Test]
    public void GetById()
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof (MySagaData))
        };
        using (var testDatabase = new SagaDatabase(sagaDefinition))
        {
            var persister = testDatabase.Persister;
            var id = Guid.NewGuid();
            var sagaData = new MySagaData
            {
                Id = id,
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                Property = "theProperty"
            };

            var storageSession = new StorageSession(testDatabase.SqlConnection, testDatabase.SqlTransaction, true);
            persister.Save(sagaData, null, storageSession, null).Await();
            var result = persister.Get<MySagaData>(id, storageSession, null).Result;
            ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
        }
    }

    public class MySagaData : XmlSagaData
    {
        public string Property { get; set; }
    }

    [Test]
    public void GetByMapping()
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof (MySagaData)),
            CorrelationMember = "Property"
        };
        using (var testDatabase = new SagaDatabase(sagaDefinition))
        {
            var persister = testDatabase.Persister;
            var id = Guid.NewGuid();
            var sagaData = new MySagaData
            {
                Id = id,
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                Property = "theProperty"
            };
            var storageSession = new StorageSession(testDatabase.SqlConnection, testDatabase.SqlTransaction, true);
            persister.Save(sagaData, null, storageSession, null).Await();
            var result = persister.Get<MySagaData>("Property", "theProperty", storageSession, null).Result;
            ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
        }
    }

    [Test]
    //TODO:
    public async Task SaveDuplicateShouldThrow()
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof (MySagaData)),
            CorrelationMember = "Property"
        };
        using (var testDatabase = new SagaDatabase(sagaDefinition))
        {
            var storageSession = new StorageSession(testDatabase.SqlConnection, testDatabase.SqlTransaction, true);
            var persister = testDatabase.Persister;
            var sagaData1 = new MySagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                Property = "theProperty"
            };
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