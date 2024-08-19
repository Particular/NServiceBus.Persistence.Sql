using System;
using System.IO;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;


[TestFixture]
public class EnsureSqlSagaNotDecoratedWithSqlSaga
{
    [Test]
    public void ThrowIfAttributeExists()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var readerParameters = new ReaderParameters(ReadingMode.Deferred);
        var module = ModuleDefinition.ReadModule(path, readerParameters);
        var dataType = module.GetTypeDefinition<SagaDecoratedWithSqlSaga>();

        var ex = Assert.Throws<Exception>(() => SagaDefinitionReader.TryGetSagaDefinition(dataType, out _));
        Assert.That(ex.Message, Does.Contain("attribute is invalid"));
    }

    [SqlSaga]
    public class SagaDecoratedWithSqlSaga : SqlSaga<SagaDecoratedWithSqlSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override string CorrelationPropertyName => "Correlation";

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }
}