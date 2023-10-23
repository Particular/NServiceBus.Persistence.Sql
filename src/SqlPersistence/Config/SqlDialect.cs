namespace NServiceBus
{
    using System.Data;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows for configuring which database engine to target. Used by <see cref="SqlPersistenceConfig.SqlDialect{T}"/>.
    /// </summary>
    public abstract partial class SqlDialect
    {
        internal string Name => GetType().Name;

        /// <summary>
        /// Gets the name of the SqlDialect.
        /// </summary>
        public override string ToString()
        {
            return Name;
        }

        internal virtual void AddCreationScriptParameters(DbCommand command)
        {
        }

        internal void AddJsonParameter(DbParameter parameter, string paramName, object value)
        {
            parameter.ParameterName = paramName;
            SetJsonParameterValue(parameter, value);
        }

        internal abstract void SetJsonParameterValue(DbParameter parameter, object value);

        internal void AddParameter(DbParameter parameter, string paramName, object value, int lengthMax)
        {
            parameter.ParameterName = paramName;
            SetParameterValue(parameter, value, lengthMax);
        }

        internal abstract void SetParameterValue(DbParameter parameter, object value, int lengthMax);

        internal abstract CommandWrapper CreateCommand(DbConnection connection);
        internal async Task ExecuteTableCommand(DbConnection connection, DbTransaction transaction, string script, string tablePrefix, CancellationToken cancellationToken = default)
        {
            //TODO: catch DbException "Parameter XXX must be defined" for mysql
            // throw and hint to add 'Allow User Variables=True' to connection string
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = script;
                command.AddParameter("tablePrefix", tablePrefix);
                AddCreationScriptParameters(command);
                _ = await command.ExecuteNonQueryEx(cancellationToken).ConfigureAwait(false);
            }
        }

        internal abstract CommandBehavior ModifyBehavior(DbConnection connection, CommandBehavior baseBehavior);

        internal virtual void ValidateTablePrefix(string tablePrefix)
        {
        }

        internal abstract object GetCustomDialectDiagnosticsInfo();
    }
}