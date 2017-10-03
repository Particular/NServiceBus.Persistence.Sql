namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
#if RELEASE
    using NUnit.Framework;
    // So this test does not run on CI as server install does not support unicode
    [Explicit("MySqlUnicode")]
#endif
    partial class When_using_special_characters_in_headers
    {
    }
}