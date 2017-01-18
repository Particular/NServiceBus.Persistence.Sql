using System;
using System.IO;
using System.Text;

namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    public static class SagaScriptBuilder
    {

        public static string BuildCreateScript(SagaDefinition saga, BuildSqlVariant sqlVariant)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildCreateScript(saga, sqlVariant, stringWriter);
            }
            return stringBuilder.ToString();
        }

        public static void BuildCreateScript(SagaDefinition saga, BuildSqlVariant sqlVariant, TextWriter writer)
        {
            Guard.AgainstNull(nameof(saga), saga);
            Guard.AgainstNull(nameof(writer), writer);

            SagaDefinitionValidator.ValidateSagaDefinition(
                correlationProperty: saga.CorrelationProperty?.Name,
                sagaName: saga.Name,
                tableSuffix: saga.TableSuffix,
                transitionalProperty: saga.TransitionalCorrelationProperty?.Name);

            var sqlVariantWriter = GetSqlVariantWriter(sqlVariant, writer, saga);

            WriteComment(writer, "TableNameVariable");
            sqlVariantWriter.WriteTableNameVariable();

            WriteComment(writer, "Initialise");
            sqlVariantWriter.Initialise();

            WriteComment(writer, "CreateTable");
            sqlVariantWriter.WriteCreateTable();
            if (saga.CorrelationProperty != null)
            {
                WriteComment(writer, $"AddProperty {saga.CorrelationProperty.Name}");
                sqlVariantWriter.AddProperty(saga.CorrelationProperty);

                WriteComment(writer, $"VerifyColumnType {saga.CorrelationProperty.Type}");
                sqlVariantWriter.VerifyColumnType(saga.CorrelationProperty);

                WriteComment(writer, $"WriteCreateIndex {saga.CorrelationProperty.Name}");
                sqlVariantWriter.WriteCreateIndex(saga.CorrelationProperty);
            }
            if (saga.TransitionalCorrelationProperty != null)
            {
                WriteComment(writer, $"AddProperty {saga.TransitionalCorrelationProperty.Name}");
                sqlVariantWriter.AddProperty(saga.TransitionalCorrelationProperty);

                WriteComment(writer, $"VerifyColumnType {saga.TransitionalCorrelationProperty.Type}");
                sqlVariantWriter.VerifyColumnType(saga.TransitionalCorrelationProperty);

                WriteComment(writer, $"CreateIndex {saga.TransitionalCorrelationProperty.Name}");
                sqlVariantWriter.WriteCreateIndex(saga.TransitionalCorrelationProperty);
            }
            WriteComment(writer, "PurgeObsoleteIndex");
            sqlVariantWriter.WritePurgeObsoleteIndex();

            WriteComment(writer, "PurgeObsoleteProperties");
            sqlVariantWriter.WritePurgeObsoleteProperties();
        }

        static void WriteComment(TextWriter writer, string text)
        {
            writer.WriteLine($@"
/* {text} */");
        }

        static ISagaScriptWriter GetSqlVariantWriter(BuildSqlVariant sqlVariant, TextWriter textWriter, SagaDefinition saga)
        {
            if (sqlVariant == BuildSqlVariant.MsSqlServer)
            {
                return new MsSqlServerSagaScriptWriter(textWriter, saga);
            }
            if (sqlVariant == BuildSqlVariant.MySql)
            {
                return new MySqlSagaScriptWriter(textWriter, saga);
            }

            throw new Exception($"Unknown SqlVariant {sqlVariant}.");
        }

        public static void BuildDropScript(SagaDefinition saga, BuildSqlVariant sqlVariant, TextWriter writer)
        {
            var sqlVariantWriter = GetSqlVariantWriter(sqlVariant, writer, saga);

            WriteComment(writer, "TableNameVariable");
            sqlVariantWriter.WriteTableNameVariable();

            WriteComment(writer, "DropTable");
            sqlVariantWriter.WriteDropTable();
        }


        public static string BuildDropScript(SagaDefinition saga, BuildSqlVariant sqlVariant)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildDropScript(saga, sqlVariant, stringWriter);
            }
            return stringBuilder.ToString();
        }
    }
}