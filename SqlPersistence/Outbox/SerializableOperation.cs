using System.Collections.Generic;

class SerializableOperation
{
    public byte[] Body;
    public Dictionary<string, string> Headers;
    public string MessageId;
    public Dictionary<string, string> Options;
}