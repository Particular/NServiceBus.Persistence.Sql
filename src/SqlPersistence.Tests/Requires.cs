class Requires
{
    public static void DtcSupport()
    {
        NUnit.Framework.Assert.Ignore("Ignoring this test because it requires DTC transaction support from the transport.");
    }
}