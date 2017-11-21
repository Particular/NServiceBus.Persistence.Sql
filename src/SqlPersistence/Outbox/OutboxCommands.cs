#pragma warning disable 1591
namespace NServiceBus.Persistence.Sql
{
    using System;

    /// <summary>
    /// Not for public use.
    /// </summary>
    [Obsolete("Not for public use")]
    [DoNotWarnAboutObsoleteUsage]
    public class OutboxCommands
    {
        public string Store { get; }
        public string Get { get; }
        public string SetAsDispatched { get; }
        public string Cleanup { get; }

        public OutboxCommands(string store, string get, string setAsDispatched, string cleanup)
        {
            Store = store;
            Get = get;
            SetAsDispatched = setAsDispatched;
            Cleanup = cleanup;
        }
    }
}