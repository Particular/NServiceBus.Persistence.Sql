namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System;
    /// <summary>
    /// Not for public use.
    /// </summary>
    public static class PostgreSqlCorrelationPropertyTypeConverter
    {
        public static string GetColumnType(CorrelationPropertyType propertyType)
        {
            switch (propertyType)
            {
                case CorrelationPropertyType.DateTime:
                    return "datetime";
                case CorrelationPropertyType.String:
                    return "varchar(200) character set utf8mb4";
                case CorrelationPropertyType.Int:
                    return "bigint(20)";
                case CorrelationPropertyType.Guid:
                    return "UUID";
            }
            throw new Exception($"Could not convert {propertyType}.");
        }
    }
}