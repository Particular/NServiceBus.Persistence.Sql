namespace NServiceBus
{
    using System.Data.Common;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows for configuring which database engine to target. Used by <see cref="SqlPersistenceConfig.SqlDialect{T}"/>.
    /// </summary>
    public abstract partial class SqlDialect
    {
        internal string Name => this.GetType().Name;

        /// <summary>
        /// Gets the name of the SqlDialect.
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }

        internal virtual void AddCreationScriptParametrs(DbCommand command)
        {
        }

        internal abstract void FillParameter(DbParameter parameter, string paramName, object value);
        internal abstract CommandWrapper CreateCommand(DbConnection connection);
        internal async Task ExecuteTableCommand(DbConnection connection, DbTransaction transaction, string script, string tablePrefix)
        {
            //TODO: catch   DbException   "Parameter XXX must be defined" for mysql
            // throw and hint to add 'Allow User Variables=True' to connection string
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = script;
                command.AddParameter("tablePrefix", tablePrefix);
                AddCreationScriptParametrs(command);
                await command.ExecuteNonQueryEx().ConfigureAwait(false);
            }
        }

        internal async Task ExecuteTableCommand(DbConnection connection, DbTransaction transaction, string script)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = script;
                AddCreationScriptParametrs(command);
                await command.ExecuteNonQueryEx().ConfigureAwait(false);
            }
        }

        internal virtual void ValidateTablePrefix(string tablePrefix)
        {
        }
    }
}