using System;
using System.Collections.Generic;
using NServiceBus.Saga;
using NServiceBus.SqlPersistence;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SagaPersisterTest
{
    [Test]
    public void Complete()
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof(MySagaData))
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
            persister.Save(sagaData);
            persister.Complete(sagaData);
            Assert.IsNull(persister.Get<MySagaData>(id));
        }
    }

    [Test]
    public void GetById()
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof(MySagaData))
        };
        using (var testDatabase = new SagaDatabase(sagaDefinition))
        {
            var persister = testDatabase.Persister;
            var id = Guid.NewGuid();
            persister.Save(new MySagaData
            {
                Id = id,
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                Property = "theProperty"
            });
            var result = persister.Get<MySagaData>(id);
            ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
        }
    }

    public class MySagaData : ContainSagaData
    {
        public string Property { get; set; }
    }

    [Test]
    public void GetByMapping()
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof(MySagaData)),
            MappedProperties = new List<string>
            {
                "Property"
            }
        };
        using (var testDatabase = new SagaDatabase(sagaDefinition))
        {
            var persister = testDatabase.Persister;
            var id = Guid.NewGuid();
            persister.Save(new MySagaData
            {
                Id = id,
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                Property = "theProperty"
            });
            var result = persister.Get<MySagaData>("Property", "theProperty");
            ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
        }
    }
    [Test]
    public void SaveDuplicateShouldThrow()
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof(MySagaData)),
            MappedProperties = new List<string>
            {
                "Property"
            }
        };
        using (var testDatabase = new SagaDatabase(sagaDefinition))
        {
            var persister = testDatabase.Persister;
            persister.Save(new MySagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                Property = "theProperty"
            });
            persister.Save(new MySagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                Property = "theProperty"
            });
        }
    }

}