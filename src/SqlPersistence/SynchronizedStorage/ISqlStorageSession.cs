namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Threading.Tasks;
    using System.Data.Common;
    using System.Threading;

    /// <summary>
    /// Exposes the current <see cref="DbTransaction"/> and <see cref="DbConnection"/>.
    /// </summary>
    public interface ISqlStorageSession
    {
        /// <summary>
        /// The current <see cref="DbTransaction"/>.
        /// </summary>
        DbTransaction Transaction { get; }

        /// <summary>
        /// The current <see cref="DbConnection"/>.
        /// </summary>
        DbConnection Connection { get; }

        /// <summary>
        /// Registers a callback to be called before completing the session.
        /// </summary>
        void OnSaveChanges(Func<ISqlStorageSession, CancellationToken, Task> callback);

        /// <summary>
        /// Registers a callback to be called before completing the session.
        /// </summary>
        [ObsoleteEx(Message = "Use the overload that supports cancellation.", TreatAsErrorFromVersion = "7", RemoveInVersion = "8")]
#pragma warning disable PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        void OnSaveChanges(Func<ISqlStorageSession, Task> callback);
#pragma warning restore PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
    }
}