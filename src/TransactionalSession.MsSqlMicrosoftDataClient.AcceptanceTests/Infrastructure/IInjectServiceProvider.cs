namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;

    public interface IInjectServiceProvider
    {
        IServiceProvider ServiceProvider { get; set; }
    }
}