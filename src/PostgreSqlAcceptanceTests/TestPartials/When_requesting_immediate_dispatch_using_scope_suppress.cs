namespace NServiceBus.AcceptanceTests.Tx
{
    using NUnit.Framework;

    [Explicit("SQL transport does not support immediate dispatch via scope suppress")]
    public partial class When_requesting_immediate_dispatch_using_scope_suppress
    {
    }
}