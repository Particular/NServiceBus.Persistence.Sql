using System;
using System.IO;
using System.Text;

namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    public static class SagaScriptBuilder
    {

        public static string BuildCreateScript(SagaDefinition saga, BuildSqlVarient sqlVarient)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildCreateScript(saga, sqlVarient, stringWriter);
            }
            return stringBuilder.ToString();
        }

        public static void BuildCreateScript(SagaDefinition saga, BuildSqlVarient sqlVarient, TextWriter writer)
        {
            Guard.AgainstNull(nameof(saga), saga);
            Guard.AgainstNull(nameof(writer), writer);

            SagaDefinitionValidator.ValidateSagaDefinition(
                correlationProperty: saga.CorrelationProperty?.Name,
                sagaName: saga.Name,
                tableSuffix: saga.TableSuffix,
                transitionalProperty: saga.TransitionalCorrelationProperty?.Name);

            var sqlVarientWriter = GetSqlVarientWriter(sqlVarient, writer, saga);

            WriteComment(writer, "TableNameVariable");
            sqlVarientWriter.WriteTableNameVariable();

            WriteComment(writer, "CreateTable");
            sqlVarientWriter.WriteCreateTable();
            if (saga.CorrelationProperty != null)
            {
                WriteComment(writer, "AddProperty " + saga.CorrelationProperty.Name);
                sqlVarientWriter.AddProperty(saga.CorrelationProperty);

                WriteComment(writer, "VerifyColumnType " + saga.CorrelationProperty.Type);
                sqlVarientWriter.VerifyColumnType(saga.CorrelationProperty);

                WriteComment(writer, "WriteCreateIndex " + saga.CorrelationProperty.Name);
                sqlVarientWriter.WriteCreateIndex(saga.CorrelationProperty);
            }
            if (saga.TransitionalCorrelationProperty != null)
            {
                WriteComment(writer, "AddProperty " + saga.TransitionalCorrelationProperty.Name);
                sqlVarientWriter.AddProperty(saga.TransitionalCorrelationProperty);

                WriteComment(writer, "VerifyColumnType " + saga.TransitionalCorrelationProperty.Type);
                sqlVarientWriter.VerifyColumnType(saga.TransitionalCorrelationProperty);

                WriteComment(writer, "CreateIndex " + saga.TransitionalCorrelationProperty.Name);
                sqlVarientWriter.WriteCreateIndex(saga.TransitionalCorrelationProperty);
            }
            WriteComment(writer, "PurgeObsoleteIndex");
            sqlVarientWriter.WritePurgeObsoleteIndex();

            WriteComment(writer, "PurgeObsoleteProperties");
            sqlVarientWriter.WritePurgeObsoleteProperties();
        }

        static void WriteComment(TextWriter writer, string text)
        {
            writer.WriteLine($@"
/* {text} */");
        }

        static ISagaScriptWriter GetSqlVarientWriter(BuildSqlVarient sqlVarient, TextWriter textWriter, SagaDefinition saga)
        {
            if (sqlVarient == BuildSqlVarient.MsSqlServer)
            {
                return new MsSqlServerSagaScriptWriter(textWriter, saga);
            }
            if (sqlVarient == BuildSqlVarient.MySql)
            {
                return new MySqlSagaScriptWriter(textWriter, saga);
            }

            throw new Exception($"Unknown SqlVarient {sqlVarient}.");
        }

        public static void BuildDropScript(SagaDefinition saga, BuildSqlVarient sqlVarient, TextWriter writer)
        {
            var sqlVarientWriter = GetSqlVarientWriter(sqlVarient, writer, saga);

            WriteComment(writer, "TableNameVariable");
            sqlVarientWriter.WriteTableNameVariable();

            WriteComment(writer, "DropTable");
            sqlVarientWriter.WriteDropTable();
        }


        public static string BuildDropScript(SagaDefinition saga, BuildSqlVarient sqlVarient)
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