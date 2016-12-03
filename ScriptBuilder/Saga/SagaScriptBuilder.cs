using System;
using System.IO;
using System.Text;

namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    public static class SagaScriptBuilder
    {

        public static string BuildCreateScript(SagaDefinition saga, SqlVarient sqlVarient)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildCreateScript(saga, sqlVarient, stringWriter);
            }
            return stringBuilder.ToString();
        }

        public static void BuildCreateScript(SagaDefinition saga, SqlVarient sqlVarient, TextWriter writer)
        {
            Guard.AgainstNull(nameof(saga), saga);
            Guard.AgainstNull(nameof(writer), writer);

            SagaDefinitionValidator.ValidateSagaDefinition(
                correlationProperty: saga.CorrelationProperty?.Name,
                sagaName: saga.Name,
                tableSuffix: saga.TableSuffix,
                transitionalProperty: saga.TransitionalCorrelationProperty?.Name);

            var sqlVarientWriter = GetSqlVarientWriter(sqlVarient, writer, saga);

            sqlVarientWriter.WriteTableNameVariable();
            sqlVarientWriter.WriteCreateTable();
            if (saga.CorrelationProperty != null)
            {
                sqlVarientWriter.AddProperty(saga.CorrelationProperty);
                sqlVarientWriter.VerifyColumnType(saga.CorrelationProperty);
                sqlVarientWriter.WriteCreateIndex(saga.CorrelationProperty);
            }
            if (saga.TransitionalCorrelationProperty != null)
            {
                sqlVarientWriter.AddProperty(saga.TransitionalCorrelationProperty);
                sqlVarientWriter.VerifyColumnType(saga.TransitionalCorrelationProperty);
                sqlVarientWriter.WriteCreateIndex(saga.TransitionalCorrelationProperty);
            }
            sqlVarientWriter.WritePurgeObsoleteIndex();
            sqlVarientWriter.WritePurgeObsoleteProperties();
        }

        static ISagaScriptWriter GetSqlVarientWriter(SqlVarient sqlVarient, TextWriter textWriter, SagaDefinition saga)
        {
            if (sqlVarient == SqlVarient.MsSqlServer)
            {
                return new MsSqlServerSagaScriptWriter(textWriter, saga);
            }
            //if (sqlVarient == SqlVarient.PostgreSql)
            //{
            //    return new PostgreSqlSagaScriptWriter(textWriter, saga);
            //}
            if (sqlVarient == SqlVarient.MySql)
            {
                return new PostgreSqlSagaScriptWriter(textWriter, saga);
            }

            throw new Exception($"Unknown SqlVarient {sqlVarient}.");
        }

        public static void BuildDropScript(SagaDefinition saga, SqlVarient sqlVarient, TextWriter writer)
        {
            var sqlVarientWriter = GetSqlVarientWriter(sqlVarient, writer, saga);
            sqlVarientWriter.WriteTableNameVariable();
            sqlVarientWriter.WriteDropTable();
        }


        public static string BuildDropScript(SagaDefinition saga, SqlVarient sqlVarient)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildDropScript(saga, sqlVarient, stringWriter);
            }
            return stringBuilder.ToString();
        }
    }
}