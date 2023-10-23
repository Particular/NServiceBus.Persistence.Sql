﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;

class CommandWrapper : IDisposable
{
    protected DbCommand command;
    List<CharArrayTextWriter> writers;
    SqlDialect dialect;
    int disposeSignaled;

    public CommandWrapper(DbCommand command, SqlDialect dialect)
    {
        this.command = command;
        this.dialect = dialect;
    }

    public DbCommand InnerCommand => command;

    public string CommandText
    {
        get => command.CommandText;
        set => command.CommandText = value;
    }

    public DbTransaction Transaction
    {
        get => command.Transaction;
        set => command.Transaction = value;
    }

    public void AddParameter(string name, object value, int lengthMax)
    {
        var parameter = command.CreateParameter();
        dialect.AddParameter(parameter, name, value, lengthMax);
        command.Parameters.Add(parameter);
    }

    public void AddJsonParameter(string name, object value)
    {
        var parameter = command.CreateParameter();
        dialect.AddJsonParameter(parameter, name, value);
        command.Parameters.Add(parameter);
    }

    public void AddParameter(string name, Version value, int lengthMax)
    {
        AddParameter(name, value.ToString(), lengthMax);
    }

    public Task<int> ExecuteNonQueryEx(CancellationToken cancellationToken = default)
    {
        return command.ExecuteNonQueryEx(cancellationToken);
    }

    public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default)
    {
        return command.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default)
    {
        return command.ExecuteReaderAsync(cancellationToken);
    }

    public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default)
    {
        var resultingBehavior = dialect.ModifyBehavior(command.Connection, behavior);
        return command.ExecuteReaderAsync(resultingBehavior, cancellationToken);
    }

    public CharArrayTextWriter LeaseWriter()
    {
        var writer = CharArrayTextWriter.Lease();
        writers ??= [];
        writers.Add(writer);
        return writer;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
        {
            return;
        }
        var temp = Interlocked.Exchange(ref command, null);
        temp?.Dispose();

        var tempWriters = Interlocked.Exchange(ref writers, null);
        if (tempWriters != null)
        {
            foreach (var writer in tempWriters)
            {
                writer.Release();
            }
        }
    }
}