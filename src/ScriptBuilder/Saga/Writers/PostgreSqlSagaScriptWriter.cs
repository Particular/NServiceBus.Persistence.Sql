using System.IO;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class PostgreSqlSagaScriptWriter : ISagaScriptWriter
{
    TextWriter writer;
    SagaDefinition saga;

    public PostgreSqlSagaScriptWriter(TextWriter textWriter, SagaDefinition saga)
    {
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
        script = 'alter table public.""' || tableNameNonQuoted || '"" add column if not exists ""Correlation_{name}"" {columnType}';
        execute script;
");
    }

    public void VerifyColumnType(CorrelationProperty correlationProperty)
    {
        var columnType = PostgreSqlCorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        var name = correlationProperty.Name;
        writer.Write($@"
        columnType := (
            select data_type || ' ' || coalesce(' (' || character_maximum_length || ')', '')
            from information_schema.columns
            where
            table_name = 'tableNameNonQuoted' and
            column_name = 'Correlation_{name}'
        );
        if columnType <> '{columnType}' then
            raise exception 'Incorrect data type for Correlation_{name}. Expected {columnType} got %', columnType;
        end if;
");
    }

    public void WriteCreateIndex(CorrelationProperty correlationProperty)
    {
        writer.Write($@"
        script = 'create unique index if not exists ""Index_Correlation_{correlationProperty.Name}"" on public.""' || tableNameNonQuoted || '"" using btree (""Correlation_{correlationProperty.Name}"" asc);';
        execute script;"
);
    }

    public void WritePurgeObsoleteIndex()
    {
        // Dropping the column with the index attached removes the index as well
    }

    public void WritePurgeObsoleteProperties()
    {
    }


    public void WriteCreateTable()
    {
        var sagaName = saga.TableSuffix.Replace(' ', '_');
        writer.Write($@"
create or replace function pg_temp.create_saga_table_{sagaName}(tablePrefix varchar)
    returns integer as
    $body$
    declare
        tableNameNonQuoted varchar;
        script text;
        count int;
        columnType varchar;
    begin
        tableNameNonQuoted := tablePrefix || '{saga.TableSuffix}';
        script = 'create table if not exists public.""' || tableNameNonQuoted || '""
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

select pg_temp.create_saga_table_{sagaName}(@tablePrefix);
");
    }

    public void WriteDropTable()
    {
        var sagaName = saga.TableSuffix.Replace(' ', '_');
        writer.Write(
$@"create or replace function pg_temp.drop_saga_table_{sagaName}(tablePrefix varchar)
    returns integer as
    $body$
    declare
        tableNameNonQuoted varchar;
        dropTable text;
    begin
        tableNameNonQuoted := tablePrefix || '{saga.TableSuffix}';
        dropTable = 'drop table if exists public.""' || tableNameNonQuoted || '"";';
        execute dropTable;
        return 0;
    end;
    $body$
    language 'plpgsql';

select pg_temp.drop_saga_table_{sagaName}(@tablePrefix);
");
    }
}