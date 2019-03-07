using System;
using System.Data.Common;
using NServiceBus;

class InstallerSettings
{
    public bool Disabled { get; set; }
    public Func<Type, DbConnection> ConnectionBuilder { get; set; }
    public SqlDialect Dialect { get; set; }
    public string ScriptDirectory { get; set; }
    public string TablePrefix { get; set; }
    public bool IsMultiTenant { get; set; }
}