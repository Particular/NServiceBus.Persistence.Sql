namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System;
    using System.IO;
    using System.Text;

    public static class SagaScriptBuilder
    {
        public static string BuildCreateScript(SagaDefinition saga, BuildSqlDialect sqlDialect)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildCreateScript(saga, sqlDialect, stringWriter);
            }
            return stringBuilder.ToString();
        }

        public static void BuildCreateScript(SagaDefinition saga, BuildSqlDialect sqlDialect, TextWriter writer)
        {
            Guard.AgainstNull(nameof(saga), saga);
            Guard.AgainstNull(nameof(writer), writer);

            SagaDefinitionValidator.ValidateSagaDefinition(
                correlationProperty: saga.CorrelationProperty?.Name,
                sagaName: saga.Name,
                tableSuffix: saga.TableSuffix,
                transitionalProperty: saga.TransitionalCorrelationProperty?.Name);

            var sqlDialectWriter = GetSqlDialectWriter(sqlDialect, writer, saga);

            WriteComment(writer, "TableNameVariable");
            sqlDialectWriter.WriteTableNameVariable();

            WriteComment(writer, "Initialize");
            sqlDialectWriter.Initialize();

            WriteComment(writer, "CreateTable");
            sqlDialectWriter.WriteCreateTable();
            if(saga.CorrelationProperty != null)
            {
                WriteComment(writer, $"AddProperty {saga.CorrelationProperty.Name}");
                sqlDialectWriter.AddProperty(saga.CorrelationProperty);

                WriteComment(writer, $"VerifyColumnType {saga.CorrelationProperty.Type}");
                sqlDialectWriter.VerifyColumnType(saga.CorrelationProperty);

                WriteComment(writer, $"WriteCreateIndex {saga.CorrelationProperty.Name}");
                sqlDialectWriter.WriteCreateIndex(saga.CorrelationProperty);
            }
            if (saga.TransitionalCorrelationProperty != null)
            {
                WriteComment(writer, $"AddProperty {saga.TransitionalCorrelationProperty.Name}");
                sqlDialectWriter.AddProperty(saga.TransitionalCorrelationProperty);

                WriteComment(writer, $"VerifyColumnType {saga.TransitionalCorrelationProperty.Type}");
                sqlDialectWriter.VerifyColumnType(saga.TransitionalCorrelationProperty);

                WriteComment(writer, $"CreateIndex {saga.TransitionalCorrelationProperty.Name}");
                sqlDialectWriter.WriteCreateIndex(saga.TransitionalCorrelationProperty);
            }
            WriteComment(writer, "PurgeObsoleteIndex");
            sqlDialectWriter.WritePurgeObsoleteIndex();

            WriteComment(writer, "PurgeObsoleteProperties");
            sqlDialectWriter.WritePurgeObsoleteProperties();

            WriteComment(writer, "CompleteSagaScript");
            sqlDialectWriter.CreateComplete();
        }

        static void WriteComment(TextWriter writer, string text)
        {
            writer.WriteLine($@"
/* {text} */");
        }

        static ISagaScriptWriter GetSqlDialectWriter(BuildSqlDialect sqlDialect, TextWriter textWriter, SagaDefinition saga)
        {
            if (sqlDialect == BuildSqlDialect.MsSqlServer)
            {
                return new MsSqlServerSagaScriptWriter(textWriter, saga);
            }
            if (sqlDialect == BuildSqlDialect.MySql)
            {
                return new MySqlSagaScriptWriter(textWriter, saga);
            }
            if (sqlDialect == BuildSqlDialect.Oracle)
            {
                return new OracleSagaScriptWriter(textWriter, saga);
            }

            throw new Exception($"Unknown SqlDialect {sqlDialect}.");
        }

        public static void BuildDropScript(SagaDefinition saga, BuildSqlDialect sqlDialect, TextWriter writer)
        {
            var sqlDialectWriter = GetSqlDialectWriter(sqlDialect, writer, saga);

            WriteComment(writer, "TableNameVariable");
            sqlDialectWriter.WriteTableNameVariable();

            WriteComment(writer, "DropTable");
            sqlDialectWriter.WriteDropTable();
        }

        public static string BuildDropScript(SagaDefinition saga, BuildSqlDialect sqlDialect)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                BuildDropScript(saga, sqlDialect, stringWriter);
            }
            return stringBuilder.ToString();
        }
    }
}