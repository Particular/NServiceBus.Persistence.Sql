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
        writer.Write(@"
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @tablePrefix + '{0}]';
", saga.TableSuffix);
    }

    public void AddProperty(CorrelationProperty correlationProperty)
    {
        var columnType = CorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        writer.Write($@"
if not exists
(
  select * from sys.columns
  where
    name = 'Correlation_{correlationProperty.Name}' and
    object_id = object_id(@tableName)
)
begin
  declare @createColumn_{correlationProperty.Name} nvarchar(max);
  set @createColumn_{correlationProperty.Name} = '
  alter table ' + @tableName  + '
    add Correlation_{correlationProperty.Name} {columnType};
  ';
  exec(@createColumn_{correlationProperty.Name});
end
");
    }

    public void VerifyColumnType(CorrelationProperty correlationProperty)
    {
        var columnType = CorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        var correlationPropertyName = correlationProperty.Name;
        writer.Write($@"
declare @dataType_{correlationPropertyName} nvarchar(max);
set @dataType_{correlationPropertyName} = (
  select data_type
  from information_schema.columns
  where
    table_name = ' + @tableName  + ' and
    column_name = 'Correlation_{correlationPropertyName}'
);
if (@dataType_{correlationPropertyName} <> '{columnType}')
  throw 50000, 'Incorrect data type for {columnType}', 0
");
    }

    public void WriteCreateIndex(CorrelationProperty correlationProperty)
    {
        writer.Write($@"
if not exists
(
    select *
    from sys.indexes
    where
        name = 'Index_Correlation_{correlationProperty.Name}' and
        object_id = object_id(@tableName)
)
begin
  declare @createIndex_{correlationProperty.Name} nvarchar(max);
  set @createIndex_{correlationProperty.Name} = '
  create unique nonclustered index Index_Correlation_{correlationProperty.Name}
  on ' + @tableName  + '(Correlation_{correlationProperty.Name})
  where Correlation_{correlationProperty.Name} is not null;
';
  exec(@createIndex_{correlationProperty.Name});
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
    select 'alter table ' + @tableName  + ' drop column ' + col.column_name '; '
    from information_schema.columns col
    where
        col.table_name = ' + @tableName  + ' and
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
            builder.Append($" and\r\n        ix.Name <> 'Index_Correlation_{saga.CorrelationProperty.Name}'");
        }
        if (saga.TransitionalCorrelationProperty != null)
        {
            builder.Append($" and\r\n        ix.Name <> 'Index_Correlation_{saga.TransitionalCorrelationProperty.Name}'");
        }

        writer.Write($@"
declare @dropIndexQuery nvarchar(max);
select @dropIndexQuery =
(
    select 'drop index ' + ix.name + ' on ' + @tableName + '; '
    from sysindexes ix
    where
        ix.Id = (select object_id from sys.objects where name = @tableName) and
        ix.Name is not null and
        ix.Name like 'Index_Correlation_%'{builder}
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
        [Id] [uniqueidentifier] not null primary key,
        [Originator] [nvarchar](255),
        [OriginalMessageId] [nvarchar](255),
        [Data] [nvarchar](max) not null,
        [PersistenceVersion] [nvarchar](23) not null,
        [SagaTypeVersion] [nvarchar](23) not null
    )
';
exec(@createTable);
end
");
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
    declare @createTable nvarchar(max);
    set @createTable = 'drop table ' + @tableName;
    exec(@createTable);
end
");
    }
}