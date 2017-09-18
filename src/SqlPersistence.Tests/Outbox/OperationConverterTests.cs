#if NET452
using System.Collections.Generic;
using NServiceBus.Outbox;
using NUnit.Framework;
using ObjectApproval;


[TestFixture]
public class OperationConverterTests
{
    [Test]
    public void ToSerializable()
    {
        var operations = new List<TransportOperation>
        {
            new TransportOperation(
                messageId: "Id1",
                options: new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                },
                body: new byte[] {0x20, 0x21},
                headers: new Dictionary<string, string>
                {
                    {
                        "HeaderKey1", "HeaderValue1"
                    }
                }
            )
        };
        var serializableOperations = operations.ToSerializable();
        ObjectApprover.VerifyWithJson(serializableOperations);
    }
}
#endif