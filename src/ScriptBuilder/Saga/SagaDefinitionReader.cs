using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SagaDefinitionReader
{

    public static bool TryGetSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        if (TryGetSqlSagaDefinition(type, out definition))
        {
            return true;
        }
        if (TryInferSagaDefinition(type, out definition))
        {
            return true;
        }

        return false;
    }

    static bool TryGetSqlSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        if (!IsSqlSaga(type))
        {
            definition = null;
            return false;
        }
        CheckIsValidSaga(type);

        var correlation = GetCorrelationPropertyName(type);
        var transitional = GetTransitionalCorrelationPropertyName(type);
        var tableSuffix = GetSqlSagaTableSuffix(type);

        SagaDefinitionValidator.ValidateSagaDefinition(correlation, type.FullName, transitional, tableSuffix);

        if (tableSuffix == null)
        {
            tableSuffix = type.Name;
        }

        var sagaDataType = GetSagaDataTypeFromSagaType(type);

        definition = new SagaDefinition
        (
            correlationProperty: BuildConstraintProperty(sagaDataType, correlation),
            transitionalCorrelationProperty: BuildConstraintProperty(sagaDataType, transitional),
            tableSuffix: tableSuffix,
            name: type.FullName
        );
        return true;
    }

    static bool TryInferSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        var baseType = type.BaseType as GenericInstanceType;
        definition = null;

        // Class must directly inherit from NServiceBus.Saga<T> so that no tricks can be pulled from an intermediate class
        if (baseType == null || !baseType.FullName.StartsWith("NServiceBus.Saga") || baseType.GenericArguments.Count != 1)
        {
            return false;
        }

        CheckIsValidSaga(type);

        string correlationId = null;
        string transitionalId = null;
        string tableSuffix = null;

        var attribute = type.GetSingleAttribute("NServiceBus.Persistence.Sql.SqlSagaAttribute");
        if (attribute != null)
        {
            var args = attribute.ConstructorArguments;
            correlationId = (string) args[0].Value;
            transitionalId = (string) args[1].Value;
            tableSuffix = (string) args[2].Value;
        }

        var sagaDataType = GetSagaDataTypeFromSagaType(type);

        if (correlationId == null)
        {
            correlationId = InferCorrelationId(type, sagaDataType);
        }

        SagaDefinitionValidator.ValidateSagaDefinition(correlationId, type.FullName, transitionalId, tableSuffix);

        if (tableSuffix == null)
        {
            tableSuffix = type.Name;
        }

        definition = new SagaDefinition
        (
            correlationProperty: BuildConstraintProperty(sagaDataType, correlationId),
            transitionalCorrelationProperty: BuildConstraintProperty(sagaDataType, transitionalId),
            tableSuffix: tableSuffix,
            name: type.FullName
        );
        return true;
    }

    static string InferCorrelationId(TypeDefinition type, TypeDefinition sagaDataType)
    {
        string correlationId = null;
        var permissiveMode = true;

        var sagaDataTypeName = sagaDataType.FullName;
        var configureMethod = type.Methods.FirstOrDefault(m => m.Name == "ConfigureHowToFindSaga");
        if (configureMethod == null)
        {
            throw new ErrorsException("Saga does not contain a ConfigureHowToFindSaga method.");
        }

        //For debugging
        //var il = String.Join(Environment.NewLine, configureMethod.Body.Instructions.Select(i => i.ToString()).ToArray());

        foreach (var instruction in configureMethod.Body.Instructions)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Call:
                    var callMethod = instruction.Operand as MethodReference;
                    if (callMethod == null)
                    {
                        throw new Exception("Can't determine method call type for MSIL instruction");
                    }

                    if (callMethod.DeclaringType.FullName == "System.Linq.Expressions.Expression")
                    {
                        if (validExpressionMethods.Contains(callMethod.Name))
                        {
                            continue;
                        }
                        if (permissiveMode && callMethod.Name == "Call")
                        {
                            continue;
                        }
                    }

                    if (callMethod.DeclaringType.FullName == "System.Type" && callMethod.Name == "GetTypeFromHandle")
                    {
                        continue;
                    }

                    if (callMethod.DeclaringType.FullName == "System.Reflection.MethodBase" && callMethod.Name == "GetMethodFromHandle")
                    {
                        continue;
                    }

                    // Any other method call is not OK, bail out
                    throw new ErrorsException("Unable to determine Saga correlation property because an unexpected method call was detected in the ConfigureHowToFindSaga method. (OpCode: call)");

                case Code.Calli:
                    // Don't know of any valid uses for this call type, bail out
                    throw new ErrorsException("Unable to determine Saga correlation property because an unexpected method call was detected in the ConfigureHowToFindSaga method. (OpCode: calli)");

                case Code.Callvirt:
                    var virtMethod = instruction.Operand as MethodReference;
                    if (virtMethod == null)
                    {
                        throw new Exception("Can't determine method call type for IL instruction");
                    }

                    // Call to mapper.ConfigureMapping<T>() is OK
                    if (virtMethod.Name == "ConfigureMapping" && virtMethod.DeclaringType.FullName.StartsWith("NServiceBus.SagaPropertyMapper"))
                    {
                        // Disable Expression.Call to be very conservative in the ToSaga() section
                        permissiveMode = false;
                        continue;
                    }

                    // Call for .ToSaga() is OK
                    if (virtMethod.Name == "ToSaga" && virtMethod.DeclaringType.FullName.StartsWith("NServiceBus.ToSagaExpression"))
                    {
                        // Re-enable expression calls now that ToSaga() section is complete
                        permissiveMode = true;
                        continue;
                    }

                    // Any other callvirt is not OK, bail out
                    throw new ErrorsException("Unable to determine Saga correlation property because an unexpected method call was detected in the ConfigureHowToFindSaga method. (OpCode: callvirt)");

                case Code.Ldtoken:
                    var methodDefinition = instruction.Operand as MethodDefinition;

                    // Some Ldtokens have operands of type TypeDefinition, for loading types
                    if (methodDefinition == null)
                    {
                        continue;
                    }

                    // The method being loaded may be on wrong type, like getter for the message property
                    if (methodDefinition.DeclaringType.FullName != sagaDataTypeName)
                    {
                        continue;
                    }

                    // If we're not getting a property, we're doing something unexpected, so bail out
                    if (!methodDefinition.Name.StartsWith("get_"))
                    {
                        throw new ErrorsException("ToSaga() expression in Saga's ConfigureHowToFindSaga method should point to a saga data property.");
                    }

                    // methodDefinition.Name is the IL for the property, i.e. "get_Correlation"
                    var instanceCorrelation = methodDefinition.Name.Substring(4);
                    if (correlationId == null)
                    {
                        correlationId = instanceCorrelation;
                    }
                    else if (instanceCorrelation != correlationId)
                    {
                        throw new ErrorsException("Saga can only have one correlation property identified by .ToSaga() expressions. Fix mappings in ConfigureHowToFindSaga to map to a single correlation property or decorate the saga with [SqlSaga] attribute.");
                    }
                    break;

                default:
                    // Any branching logic is not OK
                    if (instruction.OpCode.FlowControl == FlowControl.Branch)
                    {
                        throw new ErrorsException("Branching statements are not allowed in a ConfigureHowToFindSaga method.");
                    }
                    break;
            }
        }
        return correlationId;
    }

    static readonly string[] validExpressionMethods = new[]
    {
        "Convert",
        "Parameter",
        "Property",
        "Lambda",
        "Add",
        "Constant"
    };

    static string GetCorrelationPropertyName(TypeDefinition type)
    {
        string value;
        var property = type.GetProperty("CorrelationPropertyName");
        if (property.TryGetPropertyAssignment(out value))
        {
            return value;
        }
        throw new ErrorsException(
            $@"Only a direct string (or null) return is allowed in '{type.FullName}.CorrelationPropertyName'.
For example: protected override string CorrelationPropertyName => nameof(SagaData.TheProperty);
When all messages are mapped using finders then use the following: protected override string CorrelationPropertyName => null;");
    }


    static string GetTransitionalCorrelationPropertyName(TypeDefinition type)
    {
        string value;
        PropertyDefinition property;
        if (!type.TryGetProperty("TransitionalCorrelationPropertyName", out property))
        {
            return null;
        }
        if (property.TryGetPropertyAssignment(out value))
        {
            return value;
        }
        throw new ErrorsException(
            $@"Only a direct string return is allowed in '{type.FullName}.TransitionalCorrelationPropertyName'.
For example: protected override string TransitionalCorrelationPropertyName => nameof(SagaData.TheProperty);");
    }

    static string GetSqlSagaTableSuffix(TypeDefinition type)
    {
        string value;
        PropertyDefinition property;
        if (!type.TryGetProperty("TableSuffix", out property))
        {
            return null;
        }
        if (property.TryGetPropertyAssignment(out value))
        {
            return value;
        }
        throw new ErrorsException(
            $@"Only a direct string return is allowed in '{type.FullName}.TableSuffix'.
For example: protected override string TableSuffix => ""TheCustomTableSuffix"";");
    }

    static void CheckIsValidSaga(TypeDefinition type)
    {
        if (type.HasGenericParameters)
        {
            throw new ErrorsException($"The type '{type.FullName}' has generic parameters.");
        }
        if (type.IsAbstract)
        {
            throw new ErrorsException($"The type '{type.FullName}' is abstract.");
        }
    }

    static bool IsSqlSaga(TypeDefinition type)
    {
        var baseType = type.BaseType;
        if (baseType == null)
        {
            return false;
        }
        return baseType.Scope.Name.StartsWith("NServiceBus.Persistence.Sql") &&
               baseType.FullName.StartsWith("NServiceBus.Persistence.Sql.SqlSaga");
    }

    static TypeDefinition GetSagaDataTypeFromSagaType(TypeDefinition sagaType)
    {
        var baseType = (GenericInstanceType) sagaType.BaseType;
        var sagaDataReference = baseType.GenericArguments.Single();
        var sagaDataType = sagaDataReference as TypeDefinition;
        if (sagaDataType != null)
        {
            return sagaDataType;
        }
        throw new ErrorsException($"The type '{sagaType.FullName}' uses a SagaData type '{sagaDataReference.FullName}' that is not defined in the same assembly.");
    }

    static CorrelationProperty BuildConstraintProperty(TypeDefinition sagaDataTypeDefinition, string propertyName)
    {
        if (propertyName == null)
        {
            return null;
        }
        var propertyDefinition = sagaDataTypeDefinition.Properties.SingleOrDefault(x => x.Name == propertyName);

        if (propertyDefinition == null)
        {
            throw new ErrorsException($"Expected type '{sagaDataTypeDefinition.FullName}' to contain a property named '{propertyName}'.");
        }
        if (propertyDefinition.SetMethod == null)
        {
            throw new ErrorsException($"The type '{sagaDataTypeDefinition.FullName}' has a constraint property '{propertyName}' that is read-only.");
        }
        return BuildConstraintProperty(propertyDefinition);
    }

    static CorrelationProperty BuildConstraintProperty(PropertyDefinition member)
    {
        var propertyType = member.PropertyType;
        return new CorrelationProperty
        (
            name: member.Name,
            type: CorrelationPropertyTypeReader.GetCorrelationPropertyType(propertyType)
        );
    }

}