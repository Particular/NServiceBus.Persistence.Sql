﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class PostgreSqlSagaScriptWriter : ISagaScriptWriter
{
    TextWriter writer;
    SagaDefinition saga;

    public PostgreSqlSagaScriptWriter(TextWriter textWriter, SagaDefinition saga)
    {
        /*
         * SQLDialect's ValidateTablePrefix reserves up to 20 characters for the prefix.
         * PostgreSQL supports up to 63 character names by default.
         * This leaves 43 characters for the suffix.
         */
        if (saga.TableSuffix.Length > 43)
        {
            throw new Exception($"Saga '{saga.TableSuffix}' contains more than 43 characters, which is not supported by SQL persistence using PostgreSQL. Either disable PostgreSQL script generation using the SqlPersistenceSettings assembly attribute, shorten the name of the saga, or specify an alternate table name by overriding the SqlSaga's TableSuffix property.");
        }
        writer = textWriter;
        this.saga = saga;
    }

    public void Initialize()
    {
    }

    public void WriteTableNameVariable()
    {
    }


    public void AddProperty(CorrelationProperty correlationProperty)
    {
        var columnType = PostgreSqlCorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        var name = correlationProperty.Name;

        writer.Write($@"
        script = 'alter table ""' || schema || '"".""' || tableNameNonQuoted || '"" add column if not exists ""Correlation_{name}"" {columnType}';
        execute script;
");
    }

    public void VerifyColumnType(CorrelationProperty correlationProperty)
    {
        string columnType;
        if (correlationProperty.Type == CorrelationPropertyType.String)
        {
            columnType = "character varying";
        }
        else
        {
            columnType = PostgreSqlCorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        }

        var name = correlationProperty.Name;
        writer.Write($@"
        columnType := (
            select data_type
            from information_schema.columns
            where
            table_schema = schema and
            table_name = tableNameNonQuoted and
            column_name = 'Correlation_{name}'
        );
        if columnType <> '{columnType}' then
            raise exception 'Incorrect data type for Correlation_{name}. Expected ""{columnType}"" got ""%""', columnType;
        end if;
");
    }

    public void WriteCreateIndex(CorrelationProperty correlationProperty)
    {
        var indexName = CreateSagaIndexName(saga.TableSuffix, correlationProperty.Name);

        writer.Write($@"
        script = 'create unique index if not exists ""' || tablePrefix || '{indexName}"" on ""' || schema || '"".""' || tableNameNonQuoted || '"" using btree (""Correlation_{correlationProperty.Name}"" asc);';
        execute script;"
);
    }

    public void WritePurgeObsoleteIndex()
    {
        // Dropping the column with the index attached removes the index as well
    }

    public void WritePurgeObsoleteProperties()
    {
        var builder = new StringBuilder();

        if (saga.CorrelationProperty != null)
        {
            builder.Append($" and\r\n        column_name <> 'Correlation_{saga.CorrelationProperty.Name}'");
        }
        if (saga.TransitionalCorrelationProperty != null)
        {
            builder.Append($" and\r\n        column_name <> 'Correlation_{saga.TransitionalCorrelationProperty.Name}'");
        }
        writer.Write($@"
for columnToDelete in
(
    select column_name
    from information_schema.columns
    where
        table_name = tableNameNonQuoted and
        column_name LIKE 'Correlation_%'{builder}
)
loop
	script = '
alter table ""' || schema || '"".""' || tableNameNonQuoted || '""
drop column ""' || columnToDelete || '""';
    execute script;
end loop;
");
    }


    public void WriteCreateTable()
    {
        var sagaName = saga.TableSuffix.Replace(' ', '_');
        writer.Write($@"
create or replace function pg_temp.create_saga_table_{sagaName}(tablePrefix varchar, schema varchar)
    returns integer as
    $body$
    declare
        tableNameNonQuoted varchar;
        script text;
        count int;
        columnType varchar;
        columnToDelete text;
    begin
        tableNameNonQuoted := tablePrefix || '{saga.TableSuffix}';
        script = 'create table if not exists ""' || schema || '"".""' || tableNameNonQuoted || '""
(
    ""Id"" uuid not null,
    ""Metadata"" text not null,
    ""Data"" jsonb not null,
    ""PersistenceVersion"" character varying(23),
    ""SagaTypeVersion"" character varying(23),
    ""Concurrency"" int not null,
    primary key(""Id"")
);';
        execute script;
");
    }

    public void CreateComplete()
    {
        var sagaName = saga.TableSuffix.Replace(' ', '_');
        writer.Write($@"
        return 0;
    end;
    $body$
language 'plpgsql';

select pg_temp.create_saga_table_{sagaName}(@tablePrefix, @schema);
");
    }

    public void WriteDropTable()
    {
        var sagaName = saga.TableSuffix.Replace(' ', '_');
        writer.Write(
$@"create or replace function pg_temp.drop_saga_table_{sagaName}(tablePrefix varchar, schema varchar)
    returns integer as
    $body$
    declare
        tableNameNonQuoted varchar;
        dropTable text;
    begin
        tableNameNonQuoted := tablePrefix || '{saga.TableSuffix}';
        dropTable = 'drop table if exists ""' || schema || '"".""' || tableNameNonQuoted || '"";';
        execute dropTable;
        return 0;
    end;
    $body$
    language 'plpgsql';

select pg_temp.drop_saga_table_{sagaName}(@tablePrefix, @schema);
");
    }

    // Generates an index name like "idxc_66462BF46C9DCB70FD257D" based on
    // SHA1 hash of saga name and correlation property name
    string CreateSagaIndexName(string sagaName, string correlationPropertyName)
    {
        var sb = new StringBuilder("_i_", 43);
        var clearText = Encoding.UTF8.GetBytes($"{sagaName}/{correlationPropertyName}");
        using (var sha1 = SHA1.Create())
        {
            var hashBytes = sha1.ComputeHash(clearText);
            for (var i = 0; i < 20; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
        }
        return sb.ToString();
    }
}
