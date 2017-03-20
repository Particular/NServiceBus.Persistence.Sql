using System;
using System.Data;
using System.Data.Common;

class OracleCommandWrapper : CommandWrapper
{
    public OracleCommandWrapper(DbCommand command)
        : base(command)
    {
    }

    public override void AddParameter(string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
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
        command.Parameters.Add(parameter);
    }
}