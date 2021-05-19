#pragma warning disable 1591
#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext

namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Data.Common;

    [ObsoleteEx(
        Message = "Timeout manager is removed. Timeout storage configuration can be removed.",
        TreatAsErrorFromVersion = "7",
        RemoveInVersion = "8")]
    public class TimeoutSettings
    {
        public void ConnectionBuilder(Func<DbConnection> connectionBuilder)
        {
            throw new NotImplementedException();
        }
    }

    partial class ScriptRunner
    {
        [ObsoleteEx(
            Message = "Timeout manager is removed.",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8",
            ReplacementTypeOrMember = "Install(SqlDialect sqlDialect, string tablePrefix, Func<DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox, bool shouldInstallSagas, bool shouldInstallSubscriptions)"
        )]
        public static System.Threading.Tasks.Task Install(SqlDialect sqlDialect, string tablePrefix, Func<DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox = true, bool shouldInstallSagas = true, bool shouldInstallSubscriptions = true, bool shouldInstallTimeouts = true)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Timeout manager is removed.",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8",
            ReplacementTypeOrMember = "Install(SqlDialect sqlDialect, string tablePrefix, Func<Type, DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox, bool shouldInstallSagas, bool shouldInstallSubscriptions)"
        )]
        public static System.Threading.Tasks.Task Install(SqlDialect sqlDialect, string tablePrefix, Func<Type, DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox = true, bool shouldInstallSagas = true, bool shouldInstallSubscriptions = true, bool shouldInstallTimeouts = true)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;
    using Persistence.Sql;

    public static partial class SqlPersistenceConfig
    {
        [ObsoleteEx(
            Message = "Timeout manager is removed. Timeout storage configuration can be removed.",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8")]
        public static TimeoutSettings TimeoutSettings(this PersistenceExtensions<SqlPersistence> configuration)
        {
            throw new NotImplementedException();
        }
    }
}