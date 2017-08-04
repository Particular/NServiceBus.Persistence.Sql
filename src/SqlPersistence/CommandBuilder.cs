using System;
using System.Data.Common;
using NServiceBus;

class CommandBuilder
{
    readonly Type sqlVariant;

    public CommandBuilder(Type sqlVariant)
    {
        this.sqlVariant = sqlVariant;
    }

    public CommandWrapper CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();

        if (sqlVariant == typeof(SqlDialect.MsSqlServer) || sqlVariant == typeof(SqlDialect.MySql))
        {
            return new CommandWrapper(command);
        }
        if (sqlVariant == typeof(SqlDialect.Oracle))
        {
            return new OracleCommandWrapper(command);
        }
        throw new Exception($"Unknown SqlVariant: {sqlVariant}.");
    }
}