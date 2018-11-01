using System;
using System.Data.Common;
using NServiceBus;
using NServiceBus.Extensibility;

class InstallerSettings
{
    public bool Disabled { get; set; }
    public Func<Type, ContextBag, DbConnection> ConnectionBuilder { get; set; }
    public SqlDialect Dialect { get; set; }
    public string ScriptDirectory { get; set; }
    public string TablePrefix { get; set; }
}