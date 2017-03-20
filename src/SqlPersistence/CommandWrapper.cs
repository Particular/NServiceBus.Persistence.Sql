using System;
using System.Data.Common;
using System.Threading.Tasks;

class CommandWrapper : IDisposable
{
    protected DbCommand command;

    public CommandWrapper(DbCommand command)
    {
        this.command = command;
    }

    public string CommandText
    {
        get { return command.CommandText; }
        set { command.CommandText = value; }
    }

    public virtual void AddParameter(string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public void AddParameter(string name, Version value)
    {
        AddParameter(name, value.ToString());
    }

    public Task ExecuteNonQueryEx()
    {
        return command.ExecuteNonQueryEx();
    }

    public virtual void Dispose()
    {
    }
}