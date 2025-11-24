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
    using Particular.Obsoletes;

    [ObsoleteMetadata(
        Message = "SqlSaga is no longer supported, use the normal sagas with the new mapping API.",
        RemoveInVersion = "10",
        TreatAsErrorFromVersion = "9")]
    [Obsolete("SqlSaga is no longer supported, use the normal sagas with the new mapping API.. Will be removed in version 10.0.0.", true)]
    public abstract class SqlSaga<TSagaData>;
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member