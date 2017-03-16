using System.Text;
using System;
#pragma warning disable 1591

namespace NServiceBus.Persistence.Sql
{

    /// <summary>
    /// Not for public use.
    /// </summary>
    [Obsolete("Not for public use")]
    public class SagaCommandBuilder
    {
        readonly SqlVariant sqlVariant;

        public SagaCommandBuilder(SqlVariant sqlVariant)
        {
            this.sqlVariant = sqlVariant;
        }

        [Obsolete("The SagaCommandBuilder constructor requires the SqlVariant to generate scripts for.", true)]
        public SagaCommandBuilder()
        {
        }

        public string BuildSaveCommand(string correlationProperty, string transitionalCorrelationProperty, string tableName)
        {
            var valuesBuilder = new StringBuilder();
            var insertBuilder = new StringBuilder();

            if (correlationProperty != null)
            {
                insertBuilder.Append($",\r\n    {CorrelationPropertyName(correlationProperty)}");
                valuesBuilder.Append($",\r\n    {ParamName("CorrelationId")}");
            }
            if (transitionalCorrelationProperty != null)
            {
                insertBuilder.Append($",\r\n    {CorrelationPropertyName(transitionalCorrelationProperty)}");
                valuesBuilder.Append($",\r\n    {ParamName("TransitionalCorrelationId")}");
            }

            return $@"
insert into {tableName}
(
    Id,
    Metadata,
    Data,
    PersistenceVersion,
    SagaTypeVersion,
    Concurrency{insertBuilder}
)
values
(
    {ParamName("Id")},
    {ParamName("Metadata")},
    {ParamName("Data")},
    {ParamName("PersistenceVersion")},
    {ParamName("SagaTypeVersion")},
    1{valuesBuilder}
)";
        }


        public string BuildUpdateCommand(string transitionalCorrelationProperty, string tableName)
        {
            // no need to set CorrelationProperty since it is immutable
            var correlationSet = "";
            if (transitionalCorrelationProperty != null)
            {
                correlationSet = $",\r\n    {CorrelationPropertyName(transitionalCorrelationProperty)} = {ParamName("TransitionalCorrelationId")}";
            }

            return $@"
update {tableName}
set
    Data = {ParamName("Data")},
    PersistenceVersion = {ParamName("PersistenceVersion")},
    SagaTypeVersion = {ParamName("SagaTypeVersion")},
    Concurrency = {ParamName("Concurrency")} + 1{correlationSet}
where
    Id = {ParamName("Id")} and Concurrency = {ParamName("Concurrency")}
";
        }

        public string BuildGetBySagaIdCommand(string tableName)
        {
            return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tableName}
where Id = {ParamName("Id")}
";
        }

        public string BuildGetByPropertyCommand(string propertyName, string tableName)
        {
            return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tableName}
where {CorrelationPropertyName(propertyName)} = {ParamName("propertyValue")}
";
        }

        public string BuildCompleteCommand(string tableName)
        {
            return $@"
delete from {tableName}
where Id = {ParamName("Id")} and Concurrency = {ParamName("Concurrency")}
";
        }

        public string BuildSelectFromCommand(string tableName)
        {
            return $@"
select
    Id,
    SagaTypeVersion,
    Concurrency,
    Metadata,
    Data
from {tableName}
";
        }

        string CorrelationPropertyName(string propertyName)
        {
            switch (sqlVariant)
            {
                case SqlVariant.Oracle:
                    var oracleName = "CORR_" + propertyName.ToUpper();
                    return oracleName.Length > 30 ? oracleName.Substring(0, 30) : oracleName;
                default:
                    return "Correlation_" + propertyName;
            }
        }

        string ParamName(string name)
        {
            switch (sqlVariant)
            {
                case SqlVariant.Oracle:
                    return ":" + name;
                default:
                    return "@" + name;
            }
        }
    }
}