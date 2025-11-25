using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTests;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;

[TestFixture]
public class When_transitioning_correlation_property : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_remove_old_property_after_phase_three()
    {
        var dialect = BuildSqlDialect.Oracle;
        var sagaPhase1 = RuntimeSagaDefinitionReader.GetSagaDefinition(typeof(Phase1Saga), dialect);
        var sagaPhase2 = RuntimeSagaDefinitionReader.GetSagaDefinition(typeof(Phase2Saga), dialect);
        var sagaPhase3 = RuntimeSagaDefinitionReader.GetSagaDefinition(typeof(Phase3Saga), dialect);

        string[] phase1Schema, phase2Schema, phase3Schema;

        using (var connection = OracleConnectionBuilder.Build(disableMetadataPooling: true))
        {
            await connection.OpenAsync();

            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaPhase1, dialect), "");
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase1, dialect), "");
            phase1Schema = GetSchema(connection);

            connection.PurgeStatementCache();
        }

        using (var connection = OracleConnectionBuilder.Build(disableMetadataPooling: true))
        {
            await connection.OpenAsync();

            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase2, dialect), "");
            phase2Schema = GetSchema(connection);

            connection.PurgeStatementCache();
        }

        using (var connection = OracleConnectionBuilder.Build(disableMetadataPooling: true))
        {
            await connection.OpenAsync();

            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase3, dialect), "");
            phase3Schema = GetSchema(connection);

            connection.PurgeStatementCache();
        }

        Assert.That(phase1Schema, Has.Member("CORR_ORDERNUMBER"));
        Assert.Multiple(() =>
        {
            Assert.That(phase1Schema, Has.No.Member("CORR_ORDERID"));

            Assert.That(phase2Schema, Has.Member("CORR_ORDERNUMBER"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(phase2Schema, Has.Member("CORR_ORDERID"));

            Assert.That(phase3Schema, Has.No.Member("CORR_ORDERNUMBER"));
        });
        Assert.That(phase3Schema, Has.Member("CORR_ORDERID"));
    }

    static string[] GetSchema(OracleConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM TRANSCORRPROPSAGA";
            command.CommandType = CommandType.Text;

            using (var reader = command.ExecuteReader())
            {
                var schemaTable = reader.GetSchemaTable();
                return schemaTable.Rows.OfType<DataRow>().Select(r => (string)r[0]).ToArray();
            }
        }
    }

    #region Phase 1

    [SqlSaga(tableSuffix: "TransCorrPropSaga")]
    public class Phase1Saga : Saga<Phase1Saga.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.OrderNumber).ToMessage<StartSagaMessage>(m => m.OrderNumber);

        public class SagaData : ContainSagaData
        {
            public string OrderId { get; set; }
            public int OrderNumber { get; set; }
        }
    }

    #endregion

    #region Phase 2

    [SqlSaga(tableSuffix: "TransCorrPropSaga", transitionalCorrelationProperty: nameof(SagaData.OrderId))]
    public class Phase2Saga : Saga<Phase2Saga.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.OrderNumber).ToMessage<StartSagaMessage>(m => m.OrderNumber);

        public class SagaData : ContainSagaData
        {
            public string OrderId { get; set; }
            public int OrderNumber { get; set; }
        }
    }

    #endregion

    #region Phase 3

    [SqlSaga(tableSuffix: "TransCorrPropSaga")]
    public class Phase3Saga : Saga<Phase3Saga.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.OrderId).ToMessage<StartSagaMessage>(m => m.OrderId);

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