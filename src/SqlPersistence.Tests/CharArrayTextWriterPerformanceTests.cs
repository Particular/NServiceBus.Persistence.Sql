using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

[Explicit("This is a long running performance test")]
public class CharArrayTextWriterPerformanceTests
{
    [TestCaseSource(nameof(Dialects))]
    public void PerfTest(SqlDialect dialect)
    {
        var sagaInfo = BuildSagaInfo<S, S.SagaData>(dialect);
        var data = new S.SagaData
        {
            StringProperty = "Some bigger test",
            GuidProperty = Guid.NewGuid(),
            OriginalMessageId = Guid.NewGuid().ToString(),
            Originator = "originator",
            IntProperty = 5,
            Id = Guid.NewGuid()
        };

        // warm up
        using (var cmd = new CommandWrapper(null, dialect))
        {
            Console.WriteLine(dialect.BuildSagaData(cmd, sagaInfo, data));
        }

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 10_000_000; i++)
        {
            using (var cmd = new CommandWrapper(null, dialect))
            {
                dialect.BuildSagaData(cmd, sagaInfo, data);
            }
        }
        Console.WriteLine($"Test took: '{sw.Elapsed}'");
    }

    public class S :
        SqlSaga<S.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string StringProperty { get; set; }
            public int IntProperty { get; set; }
            public Guid GuidProperty { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.StringProperty);

        protected override string TableSuffix => "TableSuffix";

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<AMessage>(_ => _.StringId);
        }

        public Task Handle(AMessage message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }

    public class AMessage : IMessage
    {
        public string StringId { get; set; }
    }

    public static IEnumerable<ITestCaseData> Dialects()
    {
        yield return new TestCaseData(new SqlDialect.MsSqlServer()).SetName(nameof(SqlDialect.MsSqlServer));
        yield return new TestCaseData(new SqlDialect.MySql()).SetName(nameof(SqlDialect.MySql));
    }

    static RuntimeSagaInfo BuildSagaInfo<TSaga, TSagaData>(SqlDialect dialect)
        where TSaga : SqlSaga<TSagaData>
        where TSagaData : class, IContainSagaData
    {
        var sagaMetadataCollection = new SagaMetadataCollection();
        sagaMetadataCollection.Initialize([
            typeof(TSaga)
        ]);

        var infoCache = new SagaInfoCache(
            null,
            Serializer.JsonSerializer,
            readerCreator: reader => new JsonTextReader(reader),
            writerCreator: writer => new JsonTextWriter(writer),
            tablePrefix: "some",
            sqlDialect: dialect,
            metadataCollection: sagaMetadataCollection,
            nameFilter: sagaName => sagaName);
        return infoCache.GetInfo(typeof(TSagaData));
    }
}