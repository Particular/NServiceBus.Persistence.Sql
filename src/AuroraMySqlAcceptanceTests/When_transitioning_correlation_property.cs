﻿using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
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
    public void Should_remove_old_property_after_phase_three()
    {
        var dialect = BuildSqlDialect.MySql;

        using (var connection = MySqlConnectionBuilder.Build())
        {
            connection.Open();

            //HACK: Thread Sleeps required since information_schema.statistics takes some time to update
            var sagaPhase1 = RuntimeSagaDefinitionReader.GetSagaDefinition(typeof(Phase1Saga), dialect);
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaPhase1, dialect), "");
            Thread.Sleep(200);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase1, dialect), "");
            Thread.Sleep(200);
            var phase1Schema = GetSchema(connection);
            CollectionAssert.Contains(phase1Schema, "Correlation_OrderNumber");
            CollectionAssert.DoesNotContain(phase1Schema, "Correlation_OrderId");

            var sagaPhase2 = RuntimeSagaDefinitionReader.GetSagaDefinition(typeof(Phase2Saga), dialect);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase2, dialect), "");
            Thread.Sleep(200);
            var phase2Schema = GetSchema(connection);
            CollectionAssert.Contains(phase2Schema, "Correlation_OrderNumber");
            CollectionAssert.Contains(phase2Schema, "Correlation_OrderId");

            var sagaPhase3 = RuntimeSagaDefinitionReader.GetSagaDefinition(typeof(Phase3Saga), dialect);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaPhase3, dialect), "");
            Thread.Sleep(200);
            var phase3Schema = GetSchema(connection);
            CollectionAssert.DoesNotContain(phase3Schema, "Correlation_OrderNumber");
            CollectionAssert.Contains(phase3Schema, "Correlation_OrderId");
        }
    }

    static string[] GetSchema(DbConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM _TransitioningCorrelationPropertySaga";
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
        protected override string TableSuffix => "TransitioningCorrelationPropertySaga";

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
        protected override string TableSuffix => "TransitioningCorrelationPropertySaga";

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
        protected override string TableSuffix => "TransitioningCorrelationPropertySaga";

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