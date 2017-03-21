using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

class CommandWrapper : IDisposable
{
    protected DbCommand command;

    public CommandWrapper(DbCommand command)
    {
        this.command = command;
    }

    public DbCommand InnerCommand => command;

    public string CommandText
    {
        get { return command.CommandText; }
        set { command.CommandText = value; }
    }

    public DbTransaction Transaction
    {
        get { return command.Transaction; }
        set { command.Transaction = value; }
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

    public Task<int> ExecuteNonQueryAsync()
    {
        return command.ExecuteNonQueryAsync();
    }

    public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior)
    {
        return command.ExecuteReaderAsync(behavior);
    }

    public virtual void Dispose()
    {
    }
}