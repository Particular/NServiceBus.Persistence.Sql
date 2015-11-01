using System.Collections.Generic;
using ApprovalTests;
using NUnit.Framework;
using ObjectApproval;


[TestFixture]
public class HeaderSerializerTest
{
    [Test]
    public void ToXml()
    {
        var dictionary = new Dictionary<string, string> {{"TheKey", "TheValue"}};
        var xml = HeaderSerializer.ToXml(dictionary);
        Approvals.VerifyXml(xml);
    }

    [Test]
    public void FromXml()
    {
        var xml = @"<headers>
  <header key=""TheKey"">TheValue</header>
</headers>";
        ObjectApprover.VerifyWithJson(HeaderSerializer.FromString(xml));
    }
}