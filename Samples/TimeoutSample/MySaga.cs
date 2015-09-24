using System;
using NServiceBus.Logging;
using NServiceBus.Saga;

public class MySaga : Saga<MySaga.SagaData>,
    IAmStartedByMessages<StartSaga>,
    IHandleTimeouts<SagaTimout>
{
    static ILog logger = LogManager.GetLogger(typeof(MySaga));

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
    {
    }

    public void Handle(StartSaga message)
    {
        var timout = new SagaTimout
        {
            Property = "PropertyValue"
        };
        RequestTimeout(TimeSpan.FromSeconds(3),timout);
    }


    public void Timeout(SagaTimout state)
    {
        logger.Info("Timeout " + state.Property);
        MarkAsComplete();
    }
    public class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public string OrderId { get; set; }
    }
}
