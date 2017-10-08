namespace NServiceBus
{
    public partial class SqlDialect
    {
        public partial class PostgreSql
        {
            internal override string GetSagaTableName(string tablePrefix, string tableSuffix)
            {
                return $"`{tablePrefix}{tableSuffix}`";
            }

            internal override string QuoteSagaTableName(string tableName)
            {
                return tableName;
            }

            internal override string GetSagaCorrelationPropertyName(string propertyName)
            {
                return "Correlation_" + propertyName;
            }

            internal override string GetSagaParameterName(string parameterName)
            {
                return "@" + parameterName;
            }
        }
    }
}