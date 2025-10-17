#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus
{
    using System;
    using Particular.Obsoletes;

    public partial class SqlPersistence
    {
        [ObsoleteMetadata(
            Message = "The SqlPersistence class is not supposed to be instantiated directly.",
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9")]
        [Obsolete("The SqlPersistence class is not supposed to be instantiated directly.. Will be removed in version 10.0.0.", true)]
        public SqlPersistence() => throw new NotImplementedException();
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member