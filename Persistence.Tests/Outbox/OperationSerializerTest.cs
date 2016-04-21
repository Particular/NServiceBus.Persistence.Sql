using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using NServiceBus.Outbox;
using NUnit.Framework;
using ObjectApproval;


[TestFixture]
public class OperationSerializerTest
{
    [Test]
    public void ToXml()
    {
        var xml = OperationSerializer.ToXml(new List<TransportOperation>
        {
            new TransportOperation(
                messageId: "Id1",
                options: new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                },
                body: new byte[] {0x20,0x21},
                headers: new Dictionary<string, string>
                {
                    {
                        "HeaderKey1", "HeaderValue1"
                    }
                }
            )
        });
        Approvals.VerifyXml(xml);
    }

    [Test]
    public void RoundTripBytes()
    {
        var xml = OperationSerializer.ToXml(new List<TransportOperation>
        {
            new TransportOperation(
                messageId: "Id1",
                options: new Dictionary<string, string>(),
                body: new byte[] {0x20,0x21},
                headers: new Dictionary<string, string>())
        });
        var bytes = OperationSerializer.FromString(xml).First().Body;
        Assert.AreEqual(0x20,bytes[0]);
        Assert.AreEqual(0x21, bytes[1]);
    }

    [Test]
    public void FromXml()
    {
        var xml = @"<operations>
    <operation messageId=""Id1"">
      <body>ICE=</body>
      <headers>
        <header key=""HeaderKey1"">HeaderValue1</header>
      </headers>
      <options>
        <option key=""OptionKey1"">OptionValue1</option>
      </options>
    </operation>
</operations>";
        ObjectApprover.VerifyWithJson(OperationSerializer.FromString(xml));
    }
}