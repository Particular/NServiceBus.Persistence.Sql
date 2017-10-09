
class Requires
{
    public static void DtcSupport()
    {
#if !NET452
        NUnit.Framework.Assert.Ignore("Ignoring this test because it requires DTC transaction support from the transport.");
#endif
    }
}