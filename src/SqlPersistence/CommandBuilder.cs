using System;
using System.Data.Common;
using NServiceBus;

class CommandBuilder
{
    readonly SqlDialect sqlDialect;

    public CommandBuilder(SqlDialect sqlDialect)
    {
        this.sqlDialect = sqlDialect;
    }

    public CommandWrapper CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();

        if (sqlDialect is SqlDialect.MsSqlServer || sqlDialect is SqlDialect.MySql)
        {
            return new CommandWrapper(command);
        }
        if (sqlDialect is SqlDialect.Oracle)
        {
            return new OracleCommandWrapper(command);
        }
        throw new Exception($"Unknown SqlDialect: {sqlDialect.Name}.");
    }
}