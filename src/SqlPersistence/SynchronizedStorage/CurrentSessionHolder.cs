using System;
using System.Threading;
using NServiceBus.Persistence.Sql;

class CurrentSessionHolder
{
    public ISqlStorageSession Current => pipelineContext.Value.Session;

    public void SetCurrentSession(StorageSession session)
    {
        pipelineContext.Value.Session = session;
    }

    public IDisposable CreateScope()
    {
        if (pipelineContext.Value != null)
        {
            throw new InvalidOperationException("Attempt to overwrite an existing session context.");
        }
        var wrapper = new Wrapper();
        pipelineContext.Value = wrapper;
        return new Scope(this);
    }

    readonly AsyncLocal<Wrapper> pipelineContext = new AsyncLocal<Wrapper>();

    class Wrapper
    {
        public StorageSession Session;
    }

    class Scope : IDisposable
    {
        public Scope(CurrentSessionHolder sessionHolder)
        {
            this.sessionHolder = sessionHolder;
        }

        public void Dispose()
        {
            sessionHolder.pipelineContext.Value = null;
        }

        readonly CurrentSessionHolder sessionHolder;
    }

}