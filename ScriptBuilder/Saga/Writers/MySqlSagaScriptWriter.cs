using System;
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

    public void WriteTableNameVariable()
    {
        writer.WriteLine($@"
set @tableName = concat(@tablePrefix, '{saga.TableSuffix}');
set @fullTableName = concat(@schema, '.', @tableName);
");
    }

    public void AddProperty(CorrelationProperty correlationProperty)
    {
        var columnType = GetColumnType(correlationProperty.Type);
        var name = correlationProperty.Name;
        writer.Write($@"
select count(*)
into @exist
from information_schema.columns 
where table_schema = database()
      and column_name = 'Correlation_{name}'
      and table_name = @tableName;

set @query = IF(
    @exist <= 0, 
    concat('alter table ', @tableName, ' add column Correlation_{name} {columnType}'), 
    'select \'Column Exists\' status');

prepare statment from @query;
execute statment;
deallocate prepare statment;
");
    }

    public void VerifyColumnType(CorrelationProperty correlationProperty)
    {
        var columnType = GetColumnType(correlationProperty.Type);
        var name = correlationProperty.Name;
        writer.Write($@"
set @column_type_{name} = (
  select column_type
  from information_schema.columns
  where
    table_schema = database() and 
    table_name = @tableName and
    column_name = 'Correlation_{name}'
);

set @query = IF(
    @column_type_{name} <> '{columnType}', 
    'SIGNAL SQLSTATE \'45000\' SET MESSAGE_TEXT = \'Incorrect data type for Correlation_{name}\'', 
    'select \'Column Type OK\' status');

prepare statment from @query;
execute statment;
deallocate prepare statment;
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
    table_name = @tableName;

set @query = IF(
    @exist <= 0, 
    concat('create unique index Index_Correlation_{name} on ', @tableName, '(Correlation_{name})'), 
    'select \'Index Exists\' status');

prepare statment from @query;
execute statment;
deallocate prepare statment;
");
    }
    public void WritePurgeObsoleteIndex()
    {
        var builder = new StringBuilder();

        var correlation = saga.CorrelationProperty;
        if (correlation != null)
        {
            builder.Append($" and\r\n        index_name <> 'Index_Correlation_{correlation.Name}'");
        }
        var transitional = saga.TransitionalCorrelationProperty;
        if (transitional != null)
        {
            builder.Append($" and\r\n        index_name <> 'Index_Correlation_{transitional.Name}'");
        }

        writer.Write($@"
select @dropIndexQuery =
(
    select concat('drop index ', index_name, ' on ', @tableName, ';')
    from information_schema.statistics
    where
        table_name = @tableName and
        index_name like 'Index_Correlation_%'{builder} and
        table_schema = database()
);
select if (
    @dropIndexQuery is not null,
    @dropIndexQuery,
    'select ''no index to delete'';')
    into @dropIndexQuery;

prepare statment from @dropIndexQuery;
execute statment;
deallocate prepare statment;
");
    }

    public void WritePurgeObsoleteProperties()
    {
        var builder = new StringBuilder();

        var correlation = saga.CorrelationProperty;
        if (correlation != null)
        {
            builder.Append($" and\r\n        col.column_name <> 'Correlation_{correlation.Name}'");
        }
        var transitional = saga.TransitionalCorrelationProperty;
        if (transitional != null)
        {
            builder.Append($" and\r\n        col.column_name <> 'Correlation_{transitional.Name}'");
        }
        writer.Write($@"
select @dropPropertiesQuery =
(
    select concat('alter table ', @tableName, ' drop column ', col.column_name, ';')
    from information_schema.columns col
    where
        col.table_name = @tableName and
        col.column_name like 'Correlation_%'{builder}
);

select if (
    @dropPropertiesQuery is not null,
    @dropPropertiesQuery,
    'select ''no property to delete'';')
    into @dropPropertiesQuery;

prepare statment from @dropPropertiesQuery;
execute statment;
deallocate prepare statment;
");
    }


    public void WriteCreateTable()
    {
        writer.Write(@"
set @createTable = concat('
    create table if not exists ', @tableName, '(
        Id varchar(38) not null,
        Originator varchar(255),
        OriginalMessageId varchar(255),
        Data longtext not null,
        PersistenceVersion varchar(23) not null,
        SagaTypeVersion varchar(23) not null,
        primary key (`Id`)
    ) DEFAULT CHARSET=utf8;
');
prepare statment from @createTable;
execute statment;
deallocate prepare statment;
");
    }

    public void WriteDropTable()
    {
        writer.Write(@"
set @createTable = concat('drop table ', @tableName);
prepare statment from @dropTable;
execute statment;
deallocate prepare statment;
");
    }

    static string GetColumnType(CorrelationPropertyType propertyType)
    {
        switch (propertyType)
        {
            case CorrelationPropertyType.DateTime:
                return "datetime";
            case CorrelationPropertyType.DateTimeOffset:
                return "datetimeoffset";
            case CorrelationPropertyType.String:
                return "varchar(450)";
            case CorrelationPropertyType.Int:
                return "bigint";
            case CorrelationPropertyType.Guid:
                return "varchar(38)";
        }
        throw new Exception($"Could not convert {propertyType}.");
    }

}