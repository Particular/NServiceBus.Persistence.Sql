using System;
using System.IO;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;


[TestFixture]
public class EnsureSqlSagaNotDecoratedBySqlSagaAttribute
{
    [Test]
    public void ThrowIfAttributeExists()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var readerParameters = new ReaderParameters(ReadingMode.Deferred);
        var module = ModuleDefinition.ReadModule(path, readerParameters);
        var dataType = module.GetTypeDefinition<SqlSagaWithAttribute>();

        var ex = Assert.Throws<Exception>(() => SagaDefinitionReader.TryGetSagaDefinition(dataType, out _));
        Assert.IsTrue(ex.Message.Contains("attribute is invalid"));
    }

    [SqlSaga]
    public class SqlSagaWithAttribute : SqlSaga<SqlSagaWithAttribute.SagaData>
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