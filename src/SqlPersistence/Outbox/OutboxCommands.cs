namespace NServiceBus.Persistence.Sql
{
    using System;

    /// <summary>
    /// Not for public use.
    /// </summary>
    [Obsolete("Not for public use")]
    public class OutboxCommands
    {
        public string Store;
        public string Get;
        public string SetAsDispatched;

        public OutboxCommands(string store, string get, string setAsDispatched)
        {
            Store = store;
            Get = get;
            SetAsDispatched = setAsDispatched;
        }
    }
}