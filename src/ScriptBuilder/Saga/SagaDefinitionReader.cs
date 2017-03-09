using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class SagaDefinitionReader
{

    public static bool TryGetSqlSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        var typeFullName = type.FullName;
        var metadata = GetMetadataFromAttribute(type);
        if (metadata == null)
        {
            metadata = InferMetadata(type);
        }
        if (metadata == null)
        {
            if (!type.IsAbstract && type.BaseType != null)
            {
                var baseTypeFullName = type.BaseType.FullName;
                if (baseTypeFullName.StartsWith("NServiceBus.Saga"))
                {
                    throw new ErrorsException($"The type '{typeFullName}' inherits from NServiceBus.Saga but is missing a [SqlSagaAttribute].");
                }
                if (baseTypeFullName.StartsWith("NServiceBus.Persistence.Sql.SqlSaga"))
                {
                    throw new ErrorsException($"The type '{typeFullName}' inherits from NServiceBus.Persistence.Sql.SqlSaga but is missing a [SqlSagaAttribute].");
                }
            }
            definition = null;
            return false;
        }
        if (type.HasGenericParameters)
        {
            throw new ErrorsException($"The type '{typeFullName}' has a [SqlSagaAttribute] but has generic parameters.");
        }
        if (type.IsAbstract)
        {
            throw new ErrorsException($"The type '{typeFullName}' has a [SqlSagaAttribute] but has is abstract.");
        }

       
        SagaDefinitionValidator.ValidateSagaDefinition(metadata.Correlation, typeFullName, metadata.Transitional, metadata.TableSuffix);
        
        if (metadata.TableSuffix == null)
        {
            metadata.TableSuffix = type.Name;
        }

        var sagaDataType = GetSagaDataTypeFromSagaType(type);

        definition = new SagaDefinition
        (
            correlationProperty: BuildConstraintProperty(sagaDataType, metadata.Correlation),
            transitionalCorrelationProperty: BuildConstraintProperty(sagaDataType, metadata.Transitional),
            tableSuffix: metadata.TableSuffix,
            name: type.FullName
        );
        return true;
    }

    class SagaMetadata
    {
        public string Correlation;
        public string Transitional;
        public string TableSuffix;
    }

    static SagaMetadata GetMetadataFromAttribute(TypeDefinition type)
    {
        var attribute = type.GetSingleAttribute("NServiceBus.Persistence.Sql.SqlSagaAttribute");
        if (attribute == null)
        {
            return null;
        }

        var metadata = new SagaMetadata();
        var arguments = attribute.ConstructorArguments;
        metadata.Correlation = (string)arguments[0].Value;
        metadata.Transitional = (string)arguments[1].Value;
        metadata.TableSuffix = (string)arguments[2].Value;
        return metadata;
    }

    static SagaMetadata InferMetadata(TypeDefinition type)
    {
        var baseType = type.BaseType as GenericInstanceType;

        // Class must directly inherit from NServiceBus.Saga<T> so that no tricks can be pulled from an intermediate class
        if (baseType == null || !baseType.FullName.StartsWith("NServiceBus.Saga") || baseType.GenericArguments.Count != 1)
        {
            return null;
        }

        var sagaDataType = baseType.GenericArguments[0].FullName;
        var configureMethod = type.Methods.FirstOrDefault(m => m.Name == "ConfigureHowToFindSaga");
        if (configureMethod == null)
        {
            return null;
        }

        string correlationId = null;

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
                    return null;

                case Code.Calli:
                    // Don't know of any valid uses for this call type, bail out
                    return null;

                case Code.Callvirt:
                    var virtMethod = instruction.Operand as MethodReference;
                    if (virtMethod == null)
                    {
                        throw new Exception("Can't determine method call type for MSIL instruction");
                    }

                    // Call to mapper.ConfigureMapping<T>() is OK
                    if (virtMethod.Name == "ConfigureMapping" && virtMethod.DeclaringType.FullName.StartsWith("NServiceBus.SagaPropertyMapper"))
                    {
                        continue;
                    }

                    // Call for .ToSaga() is OK
                    if (virtMethod.Name == "ToSaga" && virtMethod.DeclaringType.FullName.StartsWith("NServiceBus.ToSagaExpression"))
                    {
                        continue;
                    }

                    // Any other callvirt is not OK, bail out
                    return null;

                case Code.Ldtoken:
                    var methodDefinition = instruction.Operand as MethodDefinition;

                    // Some Ldtokens have operands of type TypeDefinition, for loading types
                    if (methodDefinition == null)
                    {
                        continue;
                    }

                    // The method being loaded may be on wrong type, like getter for the message property
                    if (methodDefinition.DeclaringType.FullName != sagaDataType)
                    {
                        continue;
                    }

                    // If we're not getting a property, we're doing something unexpected, so bail out
                    if (!methodDefinition.Name.StartsWith("get_"))
                    {
                        return null;
                    }

                    // methodDefinition.Name is the MSIL for the property, i.e. "get_Correlation"
                    var instanceCorrelation = methodDefinition.Name.Substring(4);
                    if (correlationId == null)
                    {
                        correlationId = instanceCorrelation;
                    }
                    else if (instanceCorrelation != correlationId)
                    {
                        return null;
                    }
                    break;

                default:
                    // Any branching logic is not OK
                    if (instruction.OpCode.FlowControl == FlowControl.Branch)
                    {
                        return null;
                    }
                    break;
            }
        }

        return new SagaMetadata
        {
            Correlation = correlationId
        };
    }

    static readonly string[] validExpressionMethods = new[]
    {
        "Convert",
        "Parameter",
        "Property",
        "Lambda"
    };
   
    static TypeDefinition GetSagaDataTypeFromSagaType(TypeDefinition sagaType)
    {
        foreach (var method in sagaType.Methods)
        {
            var parameters = method.Parameters;
            if (method.Name == "ConfigureHowToFindSaga")
            {
                if (parameters.Count == 1)
                {
                    var parameterType = parameters[0].ParameterType;
                    var parameterTypeName = parameterType.Name;
                    if (parameterTypeName.StartsWith("SagaPropertyMapper"))
                    {
                        var genericInstanceType = (GenericInstanceType)parameterType;
                        var argument = genericInstanceType.GenericArguments.Single();
                        return ToTypeDefinition(sagaType, argument);
                    }
                }
            }
            if (method.Name == "ConfigureMapping")
            {
                if (parameters.Count == 1)
                {
                    var parameterType = parameters[0].ParameterType;
                    var parameterTypeName = parameterType.Name;
                    if (parameterTypeName.StartsWith("MessagePropertyMapper"))
                    {
                        var genericInstanceType = (GenericInstanceType)parameterType;
                        var argument = genericInstanceType.GenericArguments.Single();
                        return ToTypeDefinition(sagaType, argument);
                    }
                }
            }
        }
        throw new ErrorsException($"The type '{sagaType.FullName}' needs to override Saga.ConfigureHowToFindSaga(SagaPropertyMapper) or SqlSaga.ConfigureHowToFindSaga(MessagePropertyMapper).");
    }

    static TypeDefinition ToTypeDefinition(TypeDefinition sagaType, TypeReference argument)
    {
        var sagaDataType = argument as TypeDefinition;
        if (sagaDataType != null)
        {
            return sagaDataType;
        }
        throw new ErrorsException($"The type '{sagaType.FullName}' uses a SagaData type not defined in the same assembly.");
    }

    static CorrelationProperty BuildConstraintProperty(TypeDefinition sagaDataTypeDefinition, string propertyName)
    {
        if (propertyName == null)
        {
            return null;
        }
        var propertyDefinition = sagaDataTypeDefinition.Properties.SingleOrDefault(x => x.Name == propertyName);

        if (propertyDefinition != null)
        {
            return BuildConstraintProperty(propertyDefinition);
        }
        throw new ErrorsException($"Expected type '{sagaDataTypeDefinition.FullName}' to contain a property named '{propertyName}'.");

        //todo: verify not readonly
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