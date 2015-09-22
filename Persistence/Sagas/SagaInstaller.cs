//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Linq;
//using NServiceBus;
//using NServiceBus.Installation;
//using NServiceBus.Saga;
//using NServiceBus.SqlPersistence;

//class SagaInstaller : INeedToInstallSomething
//{
//    string connectionString;
//    Func<Type, string> tableNamingConvention;

//    public SagaInstaller(string connectionString,Func<Type, string> tableNamingConvention)
//    {
//        this.connectionString = connectionString;
//        this.tableNamingConvention = tableNamingConvention;
//    }

//    public void Install(string identity, Configure config)
//    {
//        var sagaDefinitions = GetSagaDefinitions(config);

//        using (var sqlConnection = new SqlConnection(connectionString))
//        {
//            var script = SagaScriptBuilder.Build("dbo", sagaDefinitions);
//            using (var command = new SqlCommand(script, sqlConnection))
//            {
//                command.ExecuteNonQuery();
//            }
//        }
//    }

//    IEnumerable<SagaDefinition> GetSagaDefinitions(Configure config)
//    {
//        foreach (var sagaType in GetSagaTypes(config))
//        {
//         //   var saga = (Saga) FormatterServices.GetUninitializedObject(sagaType);

//            yield return new SagaDefinition
//            {
//                Name = sagaType.Name,
//                UniqueProperties = GetUniquePropertyNames(sagaType)
//            };
//        }
//    }

//    static List<string> GetUniquePropertyNames(Type sagaType)
//    {
//        return UniqueAttribute.GetUniqueProperties(sagaType)
//            .Select(x=>x.Name)
//            .ToList();
//    }

//    IEnumerable<Type> GetSagaTypes(Configure configure)
//    {
//        return configure.TypesToScan
//            .Where(IsSagaClass);
//    }

//    static bool IsSagaClass(Type type)
//    {
//        return typeof(IContainSagaData).IsAssignableFrom(type)
//            && !type.IsInterface;
//    }
//}