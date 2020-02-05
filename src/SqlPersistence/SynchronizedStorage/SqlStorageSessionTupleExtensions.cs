namespace NServiceBus.Persistence.Sql
{
    using System.Data.Common;

    /// <summary>
    /// Tuple deconstructor extension method for <see cref="ISqlStorageSession"/>.
    /// </summary>
    public static class SqlStorageSessionTupleExtensions
    {
        /// <summary>
        /// Tuple deconstructor.
        /// </summary>
        /// <param name="session">Session that this deconstructor works on.</param>
        /// <param name="connection">The current database connection (first tuple argument).</param>
        /// <param name="transaction">The current database transaction (second tuple argument).</param>
        public static void Deconstruct(this ISqlStorageSession session, out DbConnection connection, out DbTransaction transaction)
        {
            connection = session.Connection;
            transaction = session.Transaction;
        }
    }
}