using System;
using NServiceBus;

public class CompleteSagaMessage : IMessage
{
    public Guid MySagaId { get; set; }
}