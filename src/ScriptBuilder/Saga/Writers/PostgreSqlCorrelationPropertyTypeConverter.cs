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
                    return "timestamp";
                case CorrelationPropertyType.String:
                    return "character varying(200)";
                case CorrelationPropertyType.Int:
                    return "int";
                case CorrelationPropertyType.Guid:
                    return "uuid";
            }
            throw new Exception($"Could not convert {propertyType}.");
        }
    }
}