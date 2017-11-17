namespace NServiceBus
{
    public partial class SqlDialect
    {
        internal abstract string GetSagaTableName(string tablePrefix, string tableSuffix);
        internal abstract string GetSagaCorrelationPropertyName(string propertyName);
        internal abstract string GetSagaParameterName(string parameterName);

        internal virtual object BuildSagaData(CommandWrapper command, RuntimeSagaInfo sagaInfo, IContainSagaData sagaData)
        {
            return sagaInfo.ToJson(sagaData);
        }
    }
}