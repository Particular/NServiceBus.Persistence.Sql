using System;
using System.IO;
using System.Text;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class MsSqlServerSagaScriptWriter : ISagaScriptWriter
{
    TextWriter writer;
    SagaDefinition saga;

    public MsSqlServerSagaScriptWriter(TextWriter textWriter, SagaDefinition saga)
    {
        writer = textWriter;
        this.saga = saga;
    }

    public void WriteTableNameVariable()
    {
        writer.WriteLine($@"
declare @tableName nvarchar(max) = @tablePrefix + '{saga.TableSuffix}';");
    }

    public void AddProperty(CorrelationProperty correlationProperty)
    {
        var columnType = GetColumnType(correlationProperty.Type);
        var name = correlationProperty.Name;
        writer.Write($@"
if not exists
(
  select * from sys.columns
  where
    name = 'Correlation_{name}' and
    object_id = object_id(@tableName)
)
begin
  declare @createColumn_{name} nvarchar(max);
  set @createColumn_{name} = '
  alter table ' + @tableName + '
    add Correlation_{name} {columnType};
  ';
  exec(@createColumn_{name});
end
");
    }

    public void VerifyColumnType(CorrelationProperty correlationProperty)
    {
        var columnType = GetColumnType(correlationProperty.Type);
        var name = correlationProperty.Name;
        writer.Write($@"
declare @dataType_{name} nvarchar(max);
set @dataType_{name} = (
  select data_type
  from information_schema.columns
  where
    table_name = ' + @tableName + ' and
    column_name = 'Correlation_{name}'
);
if (@dataType_{name} <> '{columnType}')
  throw 50000, 'Incorrect data type for Correlation_{name}', 0
");
    }

    public void WriteCreateIndex(CorrelationProperty correlationProperty)
    {
        var name = correlationProperty.Name;
        writer.Write($@"
if not exists
(
    select *
    from sys.indexes
    where
        name = 'Index_Correlation_{name}' and
        object_id = object_id(@tableName)
)
begin
  declare @createIndex_{name} nvarchar(max);
  set @createIndex_{name} = '
  create unique index Index_Correlation_{name}
  on ' + @tableName + '(Correlation_{name})
  where Correlation_{name} is not null;
';
  exec(@createIndex_{name});
end
");
    }

    public void WritePurgeObsoleteProperties()
    {
        var builder = new StringBuilder();

        if (saga.CorrelationProperty != null)
        {
            builder.Append($" and\r\n        col.column_name <> 'Correlation_{saga.CorrelationProperty.Name}'");
        }
        if (saga.TransitionalCorrelationProperty != null)
        {
            builder.Append($" and\r\n        col.column_name <> 'Correlation_{saga.TransitionalCorrelationProperty.Name}'");
        }
        writer.Write($@"
declare @dropPropertiesQuery nvarchar(max);
select @dropPropertiesQuery =
(
    select 'alter table ' + @tableName + ' drop column ' + col.column_name ';'
    from information_schema.columns col
    where
        col.table_name = @tableName and
        col.column_name like 'Correlation_%'{builder}
);
exec sp_executesql @dropPropertiesQuery
");
    }

    public void WritePurgeObsoleteIndex()
    {
        var builder = new StringBuilder();

        if (saga.CorrelationProperty != null)
        {
            builder.Append($" and\r\n        Name <> 'Index_Correlation_{saga.CorrelationProperty.Name}'");
        }
        if (saga.TransitionalCorrelationProperty != null)
        {
            builder.Append($" and\r\n        Name <> 'Index_Correlation_{saga.TransitionalCorrelationProperty.Name}'");
        }

        writer.Write($@"
declare @dropIndexQuery nvarchar(max);
select @dropIndexQuery =
(
    select 'drop index ' + name + ' on ' + @tableName + ';'
    from sysindexes
    where
        Id = (select object_id from sys.objects where name = @tableName) and
        Name is not null and
        Name like 'Index_Correlation_%'{builder}
);
exec sp_executesql @dropIndexQuery
");
    }

    public void WriteCreateTable()
    {
        writer.Write(@"
if not exists
(
    select *
    from sys.objects
    where
        object_id = object_id(@tableName) and
        type in ('U')
)
begin
declare @createTable nvarchar(max);
set @createTable = '
    create table ' + @tableName + '(
        Id uniqueidentifier not null primary key,
        Originator nvarchar(255),
        OriginalMessageId nvarchar(255),
        Data nvarchar(max) not null,
        PersistenceVersion varchar(23) not null,
        SagaTypeVersion varchar(23) not null,
        SagaVersion int not null
    )
';
exec(@createTable);
end
");
        //TODO: move Originator and OriginalMessageId into metadata dictionary

    }

    public void WriteDropTable()
    {
        writer.Write(@"
if exists
(
    select *
    from sys.objects
    where
        object_id = object_id(@tableName)
        and type in ('U')
)
begin
    declare @dropTable nvarchar(max);
    set @dropTable = 'drop table ' + @tableName;
    exec(@dropTable);
end
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
                return "nvarchar(450)";
            case CorrelationPropertyType.Int:
                return "bigint";
            case CorrelationPropertyType.Guid:
                return "uniqueidentifier";
        }
        throw new Exception($"Could not convert {propertyType}.");
    }
}