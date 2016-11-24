using System;
using System.Collections.Generic;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureScenariosForSqlXmlPersistence : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new List<Type>();
}