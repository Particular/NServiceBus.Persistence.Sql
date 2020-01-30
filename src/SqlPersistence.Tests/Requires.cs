
class Requires
{
    public static void DtcSupport()
    {
#if !NETFRAMEWORK
        NUnit.Framework.Assert.Ignore("Ignoring this test because it requires DTC transaction support from the transport.");
#endif
    }
}