namespace NServiceBus.AcceptanceTests.Outbox
{
#if RELEASE
    using NUnit.Framework;
    // So this test does not run on CI as server install does not support unicode
    [Explicit("MySqlUnicode")]
#endif
    partial class When_headers_contain_special_characters
    {
    }
}