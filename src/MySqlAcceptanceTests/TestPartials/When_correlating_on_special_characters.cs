namespace NServiceBus.AcceptanceTests.Sagas
{
#if RELEASE
    using NUnit.Framework;
    // So this test does not run on CI as server install does not support unicode
    [Explicit("MySqlUnicode")]
#endif
    partial class When_correlating_special_chars
    {
    }
}