using System;
using System.Data.Common;
using NServiceBus;

class SqlTimeoutInstallerSettings
{
    public bool Disabled { get; set; }
    public Func<DbConnection> ConnectionBuilder { get; set; }
    public SqlDialect Dialect { get; set; }
    public string ScriptDirectory { get; set; }
    public string TablePrefix { get; set; }
}