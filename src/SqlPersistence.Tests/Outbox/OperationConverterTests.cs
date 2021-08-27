﻿using System.Collections.Generic;
using NServiceBus.Outbox;
using NUnit.Framework;
using Particular.Approvals;
using DispatchProperties = NServiceBus.Transport.DispatchProperties;

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
                properties: new DispatchProperties(new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                }),
                body: new byte[] {0x20, 0x21},
                headers: new Dictionary<string, string>
                {
                    {
                        "HeaderKey1", "HeaderValue1"
                    }
                }
            )
        };
        var serializableOperations = Serializer.Serialize(operations.ToSerializable());
        Approver.Verify(serializableOperations);
    }
}