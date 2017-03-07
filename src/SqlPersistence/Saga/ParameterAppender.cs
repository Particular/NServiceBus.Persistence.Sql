using System;
using System.Data.Common;

/// <summary>
/// Appends <see cref="DbParameter"/>s to a <see cref="DbParameterCollection"/>.
/// </summary>
/// <param name="parameterBuilder">Provides access to <see cref="DbCommand.CreateParameter"/>.</param>
/// <param name="parameterCollection">The <see cref="DbParameterCollection"/> to append to.</param>
public delegate void ParameterAppender(Func<DbParameter> parameterBuilder, DbParameterCollection parameterCollection);