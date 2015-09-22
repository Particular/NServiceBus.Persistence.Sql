using System.Collections.Generic;
using ApprovalTests;
using NUnit.Framework;
using ObjectApproval;

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
        var xml = @"<Headers>
  <Header>
    <Key>TheKey</Key>
    <Value>TheValue</Value>
  </Header>
</Headers>";
        ObjectApprover.VerifyWithJson(HeaderSerializer.FromString(xml));
    }
}