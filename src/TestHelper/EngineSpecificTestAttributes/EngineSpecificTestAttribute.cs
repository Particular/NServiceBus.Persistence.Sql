﻿using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public abstract class EngineSpecificTestAttribute : Attribute, IApplyToContext
{
    public void ApplyToContext(TestExecutionContext context)
    {
        var connection = Environment.GetEnvironmentVariable(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Ignore($"Ignoring because environment variable {ConnectionStringName} not available.");
        }
    }

    protected abstract string ConnectionStringName { get; }
}