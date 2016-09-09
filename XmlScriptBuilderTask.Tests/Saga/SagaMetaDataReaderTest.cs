using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql.Xml;
using NUnit.Framework;

[TestFixture]
public class SagaMetaDataReaderTest
{
    ModuleDefinition module;

    public SagaMetaDataReaderTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "XmlScriptBuilderTask.Tests.dll");
        module = ModuleDefinition.ReadModule(path);
    }

    [Test]
    public void FindSagaDataType()
    {
        var dataType = module.GetTypeDefinition<NestedSaga.SagaData>();
        var map= SagaMetaDataReader.BuildSagaDataMap(dataType);
        Assert.AreEqual(typeof(NestedSaga).Name.Replace("+","/"), map.Name);
    }

    [Test]
    public void ShouldDetectSagaData()
    {
        var reader = new SagaMetaDataReader(module, null);
        var sagas = reader.GetSagas().ToArray();

        Assert.IsTrue(sagas.Any(s => s.Name == "NestedSaga"));
        Assert.IsTrue(sagas.Any(s => s.Name == "NonNestedSagaData1"));
        Assert.IsTrue(sagas.Any(s => s.Name == "NonNestedSagaData2"));
    }

    public class NonNestedSagaData1 : ContainSagaData
    {
    }

    public class NonNestedSagaData2 : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

    public class NestedSaga : XmlSaga<NestedSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            [CorrelationId]
            public string  Correlation { get; set; }
        }

    }

}