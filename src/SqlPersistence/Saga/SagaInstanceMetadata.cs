using System.Collections.Generic;

class SagaInstanceMetadata
{
    public string OriginalMessageId { get; set; }
    public string Originator { get; set; }
    public List<string> CanceledTimeouts { get; set; } = new List<string>();
    public Dictionary<string, List<string>> PendingTimeouts { get; set; } = new Dictionary<string, List<string>>();
}