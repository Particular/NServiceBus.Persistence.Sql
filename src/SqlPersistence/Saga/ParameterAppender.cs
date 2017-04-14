using System;
using System.Data.Common;

/// <summary>
/// Appends <see cref="DbParameter"/>s to a <see cref="DbParameterCollection"/>.
/// </summary>
/// <param name="parameterBuilder">Provides access to <see cref="DbCommand.CreateParameter"/>.</param>
/// <param name="append">Append a <see cref="DbParameter"/> using <see cref="DbParameterCollection.Add"/> to append to.</param>
public delegate void ParameterAppender(Func<DbParameter> parameterBuilder, Action<DbParameter> append);