using System;
using System.Collections.Generic;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureScenariosForSqlPersistence : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new List<Type>();
}