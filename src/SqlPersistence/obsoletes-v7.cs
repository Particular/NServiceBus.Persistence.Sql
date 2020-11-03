#pragma warning disable 1591

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
    
    
    [ObsoleteEx(
        Message = "Timeout manager is removed.",
        TreatAsErrorFromVersion = "7",
        RemoveInVersion = "8"
        )]
    partial class ScriptRunner
    {
        [ObsoleteEx(
            Message = "Timeout manager is removed.",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8",
            ReplacementTypeOrMember = "public static System.Threading.Tasks.Task Install(NServiceBus.SqlDialect sqlDialect, string tablePrefix, System.Func<System.Data.Common.DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox = True, bool shouldInstallSagas = True, bool shouldInstallSubscriptions = True)"
        )]
        public static System.Threading.Tasks.Task Install(
            NServiceBus.SqlDialect sqlDialect,
            string tablePrefix,
            System.Func<System.Data.Common.DbConnection> connectionBuilder,
            string scriptDirectory,
            bool shouldInstallOutbox = true,
            bool shouldInstallSagas = true,
            bool shouldInstallSubscriptions = true,
            bool shouldInstallTimeouts = true)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Timeout manager is removed.",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8",
            ReplacementTypeOrMember = "public static System.Threading.Tasks.Task Install(NServiceBus.SqlDialect sqlDialect, string tablePrefix, System.Func<System.Type, System.Data.Common.DbConnection> connectionBuilder, string scriptDirectory, bool shouldInstallOutbox = True, bool shouldInstallSagas = True, bool shouldInstallSubscriptions = True)"
        )]
        public static System.Threading.Tasks.Task Install(
            NServiceBus.SqlDialect sqlDialect, 
            string tablePrefix,
            System.Func<System.Type, 
            System.Data.Common.DbConnection> connectionBuilder, 
            string scriptDirectory,
            bool shouldInstallOutbox = true, 
            bool shouldInstallSagas = true, 
            bool shouldInstallSubscriptions = true,
            bool shouldInstallTimeouts = true)
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