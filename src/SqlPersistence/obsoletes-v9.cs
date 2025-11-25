#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus
{
    using System;
    using Particular.Obsoletes;

    public partial class SqlPersistence
    {
        [ObsoleteMetadata(
            Message = "The SqlPersistence class is not supposed to be instantiated directly",
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9")]
        [Obsolete("The SqlPersistence class is not supposed to be instantiated directly. Will be removed in version 10.0.0.", true)]
        public SqlPersistence() => throw new NotImplementedException();
    }
}

namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Linq.Expressions;
    using Particular.Obsoletes;

    [ObsoleteMetadata(
        Message = "The SqlSaga base class is no longer supported, use NServiceBus.Saga<T> with the mapper.MapSaga(…).ToMessage(…) API. Other SqlSaga features can be added by decorating the saga class with SqlSagaAttribute",
        RemoveInVersion = "10",
        TreatAsErrorFromVersion = "9")]
    [Obsolete("The SqlSaga base class is no longer supported, use NServiceBus.Saga<T> with the mapper.MapSaga(…).ToMessage(…) API. Other SqlSaga features can be added by decorating the saga class with SqlSagaAttribute. Will be removed in version 10.0.0.", true)]
    public abstract class SqlSaga<TSagaData>;

    [ObsoleteMetadata(
        Message = "The SqlSaga base class is no longer supported, use NServiceBus.Saga<T> with the mapper.MapSaga(…).ToMessage(…) API. Other SqlSaga features can be added by decorating the saga class with SqlSagaAttribute",
        RemoveInVersion = "10",
        TreatAsErrorFromVersion = "9")]
    [Obsolete("The SqlSaga base class is no longer supported, use NServiceBus.Saga<T> with the mapper.MapSaga(…).ToMessage(…) API. Other SqlSaga features can be added by decorating the saga class with SqlSagaAttribute. Will be removed in version 10.0.0.", true)]
    public interface IMessagePropertyMapper
    {
        void ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty);
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member