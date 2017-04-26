using System;
using System.Data;
using System.Data.Common;

static class ParameterFiller
{
    public static void Fill(DbParameter parameter, string paramName, object value)
    {
        parameter.ParameterName = paramName;
        parameter.Value = value;
    }

    public static void OracleFill(DbParameter parameter, string paramName, object value)
    {
        parameter.ParameterName = paramName;
        if (value is Guid)
        {
            parameter.Value = value.ToString();
        }
        else if (value is Version)
        {
            parameter.DbType = DbType.String;
            parameter.Value = value.ToString();
        }
        else
        {
            parameter.Value = value;
        }
    }
}