```
// requires sql 2012 https://msdn.microsoft.com/en-us/library/jj670104.aspx
static void WriteCreateIndex(SagaDefinition saga, TextWriter writer)
{
    if (saga.CorrelationMember == null)
    {
        return;
    }
    writer.Write(@"
IF NOT EXISTS
(
SELECT *
FROM sys.indexes
WHERE
name = 'PropertyIndex_{0}' AND
object_id = OBJECT_ID(@tableName)
)
BEGIN
DECLARE @createIndex nvarchar(max);
SET @createIndex = N'
CREATE SELECTIVE XML INDEX PropertyIndex_{0}
ON ' + @tableName + '(Data)
FOR
(
{0} = ''/Data/{0}'' AS XQUERY ''xs:string'' SINGLETON
)
';
exec(@createIndex);
END
", saga.CorrelationMember);
}
```