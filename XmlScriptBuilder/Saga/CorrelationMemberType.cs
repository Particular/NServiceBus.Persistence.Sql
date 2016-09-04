namespace NServiceBus.Persistence.Sql.Xml
{
    public enum CorrelationMemberType
    {
        //nvarchar(450)
        //https://technet.microsoft.com/en-us/library/ms191241(v=sql.105).aspx
        String,
        DateTime,
        DateTimeOffset,
        Int,
        Guid
    }
}