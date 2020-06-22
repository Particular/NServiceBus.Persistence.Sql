#pragma warning disable 1591

// used by docs engine to create scripts
class OutboxCommands
{
    public string PessimisticBegin { get; }
    public string PessimisticComplete { get; }
    public string OptimisticStore { get; }
    public string Get { get; }
    public string SetAsDispatched { get; }
    public string Cleanup { get; }

    public OutboxCommands(string optimisticStore, string pessimisticBegin, string pessimisticComplete, string get, string setAsDispatched, string cleanup)
    {
        OptimisticStore = optimisticStore;
        PessimisticBegin = pessimisticBegin;
        PessimisticComplete = pessimisticComplete;
        Get = get;
        SetAsDispatched = setAsDispatched;
        Cleanup = cleanup;
    }
}