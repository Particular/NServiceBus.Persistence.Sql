using System.IO;
using Mono.Cecil;
using NServiceBus.SqlPersistence;
using NUnit.Framework;

[TestFixture]
public class SagaMetaDataReaderTest
{
    ModuleDefinition module;

    public SagaMetaDataReaderTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilderTask.Tests.dll");
        module = ModuleDefinition.ReadModule(path);
    }
    
    [Test]
    public void FindSagaDataType()
    {
        var dataType = module.GetTypeDefinition<StandardSaga.SagaData>();
        SagaDefinition map;
        string error;
        Assert.IsTrue(SagaMetaDataReader.TryBuildSagaDataMap(dataType, out map, out error));
        Assert.AreEqual(typeof(StandardSaga).FullName.Replace("+","/"), map.Name);
    }

    public class StandardSaga : XmlSaga<StandardSaga.SagaData>
    {

        public class SagaData :XmlSagaData
        {
            [CorrelationId]
            public string  Correlation { get; set; }
        }
        
    }

}