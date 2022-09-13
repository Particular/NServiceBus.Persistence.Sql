namespace NServiceBus.TransactionalSession.AcceptanceTests;

using ObjectBuilder;

public interface IInjectBuilder
{
    IBuilder Builder { get; set; }
}