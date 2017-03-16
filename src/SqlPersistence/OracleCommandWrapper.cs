using System.Data.Common;

class OracleCommandWrapper : CommandWrapper
{
    public OracleCommandWrapper(DbCommand command)
        : base(command)
    {
        var bindByNameProperty = command.GetType().GetProperty("BindByName");
        bindByNameProperty.SetValue(command, true);
    }

    public override void AddParameter(string name, object value)
    {
        var parameter = command.CreateParameter();
        ParameterFiller.OracleFill(parameter, name, value);
        command.Parameters.Add(parameter);
    }
}