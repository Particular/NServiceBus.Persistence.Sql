namespace NServiceBus
{
    public partial class SqlDialect
    {
        internal abstract string GetSagaTableName(string tablePrefix, string tableSuffix);
        internal abstract string QuoteSagaTableName(string tableName);
        internal abstract string GetSagaCorrelationPropertyName(string propertyName);
        internal abstract string GetSagaParameterName(string parameterName);
    }
}