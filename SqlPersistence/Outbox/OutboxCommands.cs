namespace NServiceBus.Persistence.Sql
{
    public class OutboxCommands
    {
        public string Store;
        public string Get;
        public string SetAsDispatched;
        public string Cleanup;

        public OutboxCommands(string store, string get, string setAsDispatched, string cleanup)
        {
            Store = store;
            Get = get;
            SetAsDispatched = setAsDispatched;
            Cleanup = cleanup;
        }
    }
}