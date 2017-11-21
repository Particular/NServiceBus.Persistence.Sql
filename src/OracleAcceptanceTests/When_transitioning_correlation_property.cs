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
            await connection.OpenAsync().ConfigureAwait(false);

            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaPhase1, dialect), "");
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase1, dialect), "");
            phase1Schema = GetSchema(connection);

            connection.PurgeStatementCache();
        }

        using (var connection = OracleConnectionBuilder.Build(disableMetadataPooling: true))
        {
            await connection.OpenAsync().ConfigureAwait(false);

            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase2, dialect), "");
            phase2Schema = GetSchema(connection);

            connection.PurgeStatementCache();
        }

        using (var connection = OracleConnectionBuilder.Build(disableMetadataPooling: true))
        {
            await connection.OpenAsync().ConfigureAwait(false);

            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase3, dialect), "");
            phase3Schema = GetSchema(connection);

            connection.PurgeStatementCache();
        }

        CollectionAssert.Contains(phase1Schema, "CORR_ORDERNUMBER");
        CollectionAssert.DoesNotContain(phase1Schema, "CORR_ORDERID");

        CollectionAssert.Contains(phase2Schema, "CORR_ORDERNUMBER");
        CollectionAssert.Contains(phase2Schema, "CORR_ORDERID");

        CollectionAssert.DoesNotContain(phase3Schema, "CORR_ORDERNUMBER");
        CollectionAssert.Contains(phase3Schema, "CORR_ORDERID");
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

    public class Phase1Saga : SqlSaga<Phase1Saga.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        protected override string CorrelationPropertyName => nameof(SagaData.OrderNumber);
        protected override string TableSuffix => "TransCorrPropSaga";

        public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(m => m.OrderNumber);
        }

        public class SagaData : ContainSagaData
        {
            public string OrderId { get; set; }
            public int OrderNumber { get; set; }
        }
    }

    #endregion

    #region Phase 2

    public class Phase2Saga : SqlSaga<Phase2Saga.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        protected override string CorrelationPropertyName => nameof(SagaData.OrderNumber);
        protected override string TransitionalCorrelationPropertyName => nameof(SagaData.OrderId);
        protected override string TableSuffix => "TransCorrPropSaga";

        public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(m => m.OrderNumber);
        }

        public class SagaData : ContainSagaData
        {
            public string OrderId { get; set; }
            public int OrderNumber { get; set; }
        }
    }

    #endregion

    #region Phase 3

    public class Phase3Saga : SqlSaga<Phase3Saga.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        protected override string CorrelationPropertyName => nameof(SagaData.OrderId);
        protected override string TableSuffix => "TransCorrPropSaga";

        public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(m => m.OrderId);
        }

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