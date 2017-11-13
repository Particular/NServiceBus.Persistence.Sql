using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql;

class InstallerSettings
{
    public bool Disabled { get; set; }
    public Func<DbConnection> ConnectionBuilder { get; set; }
    public SqlVariant SqlVariant { get; set; }
    public string ScriptDirectory { get; set; }
    public string TablePrefix { get; set; }
}