using System.IO;
using System.Text;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class MySqlSagaScriptWriter : ISagaScriptWriter
{
    TextWriter writer;
    SagaDefinition saga;

    public MySqlSagaScriptWriter(TextWriter textWriter, SagaDefinition saga)
    {
        writer = textWriter;
        this.saga = saga;
    }

    public void Initialize()
    {
        writer.WriteLine(@"
drop procedure if exists sqlpersistence_raiseerror;
create procedure sqlpersistence_raiseerror(message varchar(256))
begin
signal sqlstate
    'ERROR'
set
    message_text = message,
    mysql_errno = '45000';
end;");
    }

    public void WriteTableNameVariable()
    {
        writer.WriteLine($@"
set @tableNameQuoted = concat('`', @tablePrefix, '{saga.TableSuffix}`');
set @tableNameNonQuoted = concat(@tablePrefix, '{saga.TableSuffix}');
");
    }

    public void CreateComplete()
    {
    }

    public void AddProperty(CorrelationProperty correlationProperty)
    {
        var columnType = MySqlCorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        var name = correlationProperty.Name;
        writer.Write($@"
select count(*)
into @exist
from information_schema.columns
where table_schema = database() and
      column_name = 'Correlation_{name}' and
      table_name = @tableNameNonQuoted;

set @query = IF(
    @exist <= 0,
    concat('alter table ', @tableNameQuoted, ' add column Correlation_{name} {columnType}'), 'select \'Column Exists\' status');

prepare script from @query;
execute script;
deallocate prepare script;
");
    }

    public void VerifyColumnType(CorrelationProperty correlationProperty)
    {
        var columnType = MySqlCorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        var name = correlationProperty.Name;
        writer.Write($@"
set @column_type_{name} = (
  select concat(column_type,' character set ', character_set_name)
  from information_schema.columns
  where
    table_schema = database() and
    table_name = @tableNameNonQuoted and
    column_name = 'Correlation_{name}'
);

set @query = IF(
    @column_type_{name} <> '{columnType}',
    'call sqlpersistence_raiseerror(concat(\'Incorrect data type for Correlation_{name}. Expected {columnType} got \', @column_type_{name}, \'.\'));',
    'select \'Column Type OK\' status');

prepare script from @query;
execute script;
deallocate prepare script;
");
    }

    public void WriteCreateIndex(CorrelationProperty correlationProperty)
    {
        var name = correlationProperty.Name;
        writer.Write($@"
select count(*)
into @exist
from information_schema.statistics
where
    table_schema = database() and
    index_name = 'Index_Correlation_{name}' and
    table_name = @tableNameNonQuoted;

set @query = IF(
    @exist <= 0,
    concat('create unique index Index_Correlation_{name} on ', @tableNameQuoted, '(Correlation_{name})'), 'select \'Index Exists\' status');

prepare script from @query;
execute script;
deallocate prepare script;
");
    }

    public void WritePurgeObsoleteIndex()
    {
        var builder = new StringBuilder();

        var correlation = saga.CorrelationProperty;
        if (correlation != null)
        {
            builder.Append($" and\r\n    index_name <> 'Index_Correlation_{correlation.Name}'");
        }
        var transitional = saga.TransitionalCorrelationProperty;
        if (transitional != null)
        {
            builder.Append($" and\r\n    index_name <> 'Index_Correlation_{transitional.Name}'");
        }

        writer.Write($@"
select concat('drop index ', index_name, ' on ', @tableNameQuoted, ';')
from information_schema.statistics
where
    table_schema = database() and
    table_name = @tableNameNonQuoted and
    index_name like 'Index_Correlation_%'{builder} and
    table_schema = database()
into @dropIndexQuery;
select if (
    @dropIndexQuery is not null,
    @dropIndexQuery,
    'select ''no index to delete'';')
    into @dropIndexQuery;

prepare script from @dropIndexQuery;
execute script;
deallocate prepare script;
");
    }

    public void WritePurgeObsoleteProperties()
    {
        var builder = new StringBuilder();

        var correlation = saga.CorrelationProperty;
        if (correlation != null)
        {
            builder.Append($" and\r\n    column_name <> 'Correlation_{correlation.Name}'");
        }
        var transitional = saga.TransitionalCorrelationProperty;
        if (transitional != null)
        {
            builder.Append($" and\r\n    column_name <> 'Correlation_{transitional.Name}'");
        }
        writer.Write($@"
select concat('alter table ', table_name, ' drop column ', column_name, ';')
from information_schema.columns
where
    table_schema = database() and
    table_name = @tableNameNonQuoted and
    column_name like 'Correlation_%'{builder}
into @dropPropertiesQuery;

select if (
    @dropPropertiesQuery is not null,
    @dropPropertiesQuery,
    'select ''no property to delete'';')
    into @dropPropertiesQuery;

prepare script from @dropPropertiesQuery;
execute script;
deallocate prepare script;
");
    }


    public void WriteCreateTable()
    {
        writer.Write(@"
set @createTable = concat('
    create table if not exists ', @tableNameQuoted, '(
        Id varchar(38) not null,
        Metadata json not null,
        Data json not null,
        PersistenceVersion varchar(23) not null,
        SagaTypeVersion varchar(23) not null,
        Concurrency int not null,
        primary key (Id)
    ) default charset=utf8;
');
prepare script from @createTable;
execute script;
deallocate prepare script;
");
    }

    public void WriteDropTable()
    {
        writer.Write(@"
set @dropTable = concat('drop table if exists ', @tableNameQuoted);
prepare script from @dropTable;
execute script;
deallocate prepare script;
");
    }
}