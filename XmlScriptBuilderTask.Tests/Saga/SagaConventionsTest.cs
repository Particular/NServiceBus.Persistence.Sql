using System.IO;
using System.Linq;
using ApprovalTests;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using NServiceBus;
using NServiceBus.Persistence.Sql.Xml;
using NUnit.Framework;

[TestFixture]
public class SagaConventionsTest
{
    ModuleDefinition module;

    public SagaConventionsTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "XmlScriptBuilderTask.Tests.dll");
        module = ModuleDefinition.ReadModule(path);
    }

    [Test]
    public void BadSagaDataName()
    {
        var dataType = module.GetTypeDefinition<BadNamedSaga.BadNamedSagaData>();
        var errorsException = Assert.Throws<ErrorsException>(() => SagaMetaDataReader.ValidateSagaConventions(dataType));
        Approvals.Verify(errorsException.Message);
    }

    public class BadNamedSaga : XmlSaga<BadNamedSaga.BadNamedSagaData>
    {
        public class BadNamedSagaData : ContainSagaData
        {
        }
    }
    [Test]
    public void NestedInNonXmlSaga()
    {
        var dataType = module.GetTypeDefinition<NonXmlSaga.SagaData>();
        var errorsException = Assert.Throws<ErrorsException>(() => SagaMetaDataReader.ValidateSagaConventions(dataType));
        Approvals.Verify(errorsException.Message);
    }

    public class NonXmlSaga : Saga<NonXmlSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void WithGenericSaga()
    {
        var type = typeof (GenericSaga<>.SagaData);
        var dataType = module.GetAllTypes().First(x => x.FullName == type.FullName.Replace("+", "/"));
        var errorsException = Assert.Throws<ErrorsException>(() => SagaMetaDataReader.ValidateSagaConventions(dataType));
        Approvals.Verify(errorsException.Message);
    }

    public class GenericSaga<T> : XmlSaga<GenericSaga<T>.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }
    }

    [Test]
    public void WithGenericSagaData()
    {
        var type = typeof (GenericSagaData<>.SagaData<>);
        var dataType = module.GetAllTypes().First(x => x.FullName == type.FullName.Replace("+", "/"));
        var errorsException = Assert.Throws<ErrorsException>(() => SagaMetaDataReader.ValidateSagaConventions(dataType));
        Approvals.Verify(errorsException.Message);
    }

    public class GenericSagaData<T> : XmlSaga<GenericSagaData<T>.SagaData<T>>
    {
        public class SagaData<K> : ContainSagaData
        {
        }

    }

    [Test]
    public void Abstract()
    {
        var dataType = module.GetTypeDefinition<AbstactSaga.SagaData>();
        var errorsException = Assert.Throws<ErrorsException>(() => SagaMetaDataReader.ValidateSagaConventions(dataType));
        Approvals.Verify(errorsException.Message);
    }

    public abstract class AbstactSaga : XmlSaga<AbstactSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

    }

    [Test]
    public void MultiInheritance()
    {
        var dataType = module.GetTypeDefinition<MultiInheritanceSaga.SagaData>();
        var errorsException = Assert.Throws<ErrorsException>(() => SagaMetaDataReader.ValidateSagaConventions(dataType));
        Approvals.Verify(errorsException.Message);
    }

    public abstract class BaseSaga<T> : XmlSaga<T> where T : IContainSagaData, new()
    {

    }

    public class MultiInheritanceSaga : BaseSaga<MultiInheritanceSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

    }

    [Test]
    public void NotNestedSagaData()
    {
        var dataType = module.GetTypeDefinition<SagaData>();
        var errorsException = Assert.Throws<ErrorsException>(() => SagaMetaDataReader.ValidateSagaConventions(dataType));
        Approvals.Verify(errorsException.Message);
    }
}

public class SagaData : ContainSagaData
{
}


