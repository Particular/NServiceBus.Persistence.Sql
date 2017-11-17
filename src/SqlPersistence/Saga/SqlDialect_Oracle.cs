namespace NServiceBus
{
    using System;
    using System.Text;

    public partial class SqlDialect
    {
        public partial class Oracle
        {
            internal override string GetSagaTableName(string tablePrefix, string tableSuffix)
            {
                if (tableSuffix.Length > 27)
                {
                    throw new Exception($"Saga '{tableSuffix}' contains more than 27 characters, which is not supported by SQL persistence using Oracle. Either disable Oracle script generation using the SqlPersistenceSettings assembly attribute, shorten the name of the saga, or specify an alternate table name by overriding the SqlSaga's TableSuffix property.");
                }
                if (Encoding.UTF8.GetBytes(tableSuffix).Length != tableSuffix.Length)
                {
                    throw new Exception($"Saga '{tableSuffix}' contains non-ASCII characters, which is not supported by SQL persistence using Oracle. Either disable Oracle script generation using the SqlPersistenceSettings assembly attribute, change the name of the saga, or specify an alternate table name by overriding the SqlSaga's TableSuffix property.");
                }
                return $"{SchemaPrefix}\"{tableSuffix.ToUpper()}\"";
            }
            
            internal override string GetSagaCorrelationPropertyName(string propertyName)
            {
                var oracleName = "CORR_" + propertyName.ToUpper();
                return oracleName.Length > 30 ? oracleName.Substring(0, 30) : oracleName;
            }

            internal override string GetSagaParameterName(string parameterName)
            {
                return ":" + parameterName;
            }
        }
    }
}