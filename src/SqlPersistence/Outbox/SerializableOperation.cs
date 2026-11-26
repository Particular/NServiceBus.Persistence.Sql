using System;
using System.Collections.Generic;

sealed class SerializableOperation
{
    public ReadOnlyMemory<byte> Body { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public string MessageId { get; set; }
    public Dictionary<string, string> Options { get; set; }
}