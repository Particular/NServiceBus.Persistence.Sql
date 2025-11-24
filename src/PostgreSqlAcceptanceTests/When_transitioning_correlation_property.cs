using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTests;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class When_transitioning_correlation_property : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_remove_old_property_after_phase_three()
    {
        var dialect = BuildSqlDialect.PostgreSql;
        var sagaPhase1 = RuntimeSagaDefinitionReader.GetSagaDefinition(typeof(Phase1Saga), dialect);
        var sagaPhase2 = RuntimeSagaDefinitionReader.GetSagaDefinition(typeof(Phase2Saga), dialect);
        var sagaPhase3 = RuntimeSagaDefinitionReader.GetSagaDefinition(typeof(Phase3Saga), dialect);

        using (var connection = PostgreSqlConnectionBuilder.Build())
        {
            await connection.OpenAsync();
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaPhase1, dialect), "");
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase1, dialect), "");
            var phase1Schema = GetSchema(connection);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase2, dialect), "");
            var phase2Schema = GetSchema(connection);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase3, dialect), "");
            var phase3Schema = GetSchema(connection);

            Assert.That(phase1Schema, Has.Member("Correlation_OrderNumber"));
            Assert.Multiple(() =>
            {
                Assert.That(phase1Schema, Has.No.Member("Correlation_OrderId"));

                Assert.That(phase2Schema, Has.Member("Correlation_OrderNumber"));
            });
            Assert.Multiple(() =>
            {
                Assert.That(phase2Schema, Has.Member("Correlation_OrderId"));

                Assert.That(phase3Schema, Has.No.Member("Correlation_OrderNumber"));
            });
            Assert.That(phase3Schema, Has.Member("Correlation_OrderId"));
        }
    }

    static string[] GetSchema(DbConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "select * from \"public\".\"_TransitioningCorrelationPropertySaga\"";
            command.CommandType = CommandType.Text;

            using (var reader = command.ExecuteReader())
            {
                var schemaTable = reader.GetSchemaTable();
                return schemaTable.Rows.OfType<DataRow>().Select(r => (string)r[0]).ToArray();
            }
        }
    }

    #region Phase 1

    [SqlSaga(tableSuffix: "TransitioningCorrelationPropertySaga")]
    public class Phase1Saga : Saga<Phase1Saga.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.OrderNumber).ToMessage<StartSagaMessage>(m => m.OrderNumber);

        public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        public class SagaData : ContainSagaData
        {
            public string OrderId { get; set; }
            public int OrderNumber { get; set; }
        }
    }

    #endregion

    #region Phase 2

    [SqlSaga(tableSuffix: "TransitioningCorrelationPropertySaga", transitionalCorrelationProperty: nameof(SagaData.OrderId))]
    public class Phase2Saga : Saga<Phase2Saga.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.OrderNumber).ToMessage<StartSagaMessage>(m => m.OrderNumber);

        public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        public class SagaData : ContainSagaData
        {
            public string OrderId { get; set; }
            public int OrderNumber { get; set; }
        }
    }

    #endregion

    #region Phase 3

    [SqlSaga(tableSuffix: "TransitioningCorrelationPropertySaga")]
    public class Phase3Saga : Saga<Phase3Saga.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.OrderId).ToMessage<StartSagaMessage>(m => m.OrderId);

        public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        public class SagaData : ContainSagaData
        {
            public string OrderId { get; set; }
            public int OrderNumber { get; set; }
        }
    }

    #endregion

    public class StartSagaMessage : IMessage
    {
        public string OrderId { get; set; }
        public int OrderNumber { get; set; }
    }
}