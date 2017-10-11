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
        script = 'alter table public.' || tableNameNonQuoted || ' add column if not exists ""Correlation_{name}"" {columnType}';
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
            table_name = tableNameNonQuoted and
            column_name = 'Correlation_{name}'
        );
        if columnType <> '{columnType}' then
            raise exception 'Incorrect data type for Correlation_{name}. Expected {columnType} got %', column_type;
        end if;
");
    }

    public void WriteCreateIndex(CorrelationProperty correlationProperty)
    {
    }

    public void WritePurgeObsoleteIndex()
    {
    }

    public void WritePurgeObsoleteProperties()
    {
    }


    public void WriteCreateTable()
    {
        writer.Write($@"
create or replace function create_saga_table_{saga.TableSuffix}(tablePrefix varchar)
    returns integer as
    $body$
    declare
        tableNameNonQuoted varchar;
        script text;
        count int;
        columnType varchar;
    begin
        tableNameNonQuoted := tablePrefix || '{saga.TableSuffix}';
        script = 'create table if not exists public.' || tableNameNonQuoted || '
(
    ""Id"" uuid not null,
    ""Metadata"" text not null,
    ""Data"" jsonb not null,
    ""PersistenceVersion"" character varying(23),
    ""SagaTypeVersion"" character varying(23),
    ""Concurrency"" int not null,
    primary key(""Id"")
);';
        execute script;");
    }
    public void CreateComplete()
    {
        writer.Write($@"
        return 0;
    end;
    $body$
language 'plpgsql';

select create_saga_table_{saga.TableSuffix}(@tablePrefix);");
    }

    public void WriteDropTable()
    {
        writer.Write(
$@"create or replace function drop_saga_table_{saga.TableSuffix}(tablePrefix varchar)
    returns integer as
    $body$
    declare
        tableNameNonQuoted varchar;
        dropTable text;
    begin
        tableNameNonQuoted := tablePrefix || '{saga.TableSuffix}';
        dropTable = 'drop table if exists public.' || tableNameNonQuoted || ';';
        execute dropTable;
        return 0;
    end;
    $body$
    language 'plpgsql';

select drop_saga_table_{saga.TableSuffix}(@tablePrefix);
");
    }
}