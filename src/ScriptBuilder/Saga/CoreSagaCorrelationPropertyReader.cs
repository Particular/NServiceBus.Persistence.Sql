using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using NServiceBus.Persistence.Sql;

class CoreSagaCorrelationPropertyReader
{
    string sagaDataTypeName;
    Collection<Instruction> instructions;
    static HashSet<string> allowedMethods;

    static CoreSagaCorrelationPropertyReader()
    {
        allowedMethods = new HashSet<string>(new[]
        {
            "System.Type::GetTypeFromHandle",
            "System.Reflection.MethodBase::GetMethodFromHandle",
            "System.Linq.Expressions.Expression::Convert",
            "System.Linq.Expressions.Expression::Parameter",
            "System.Linq.Expressions.Expression::Property",
            "System.Linq.Expressions.Expression::Lambda",
            "System.Linq.Expressions.Expression::Add",
            "System.Linq.Expressions.Expression::Constant"
        }, StringComparer.Ordinal);
    }

    public CoreSagaCorrelationPropertyReader(TypeDefinition type, TypeDefinition sagaDataType)
    {
        sagaDataTypeName = sagaDataType.FullName;
        var configureMethod = type.Methods.FirstOrDefault(m => m.Name == "ConfigureHowToFindSaga");
        if (configureMethod == null)
        {
            throw new ErrorsException("Saga does not contain a ConfigureHowToFindSaga method.");
        }

        this.instructions = configureMethod.Body.Instructions;
    }

    public bool ContainsBranchingLogic()
    {
        return instructions.Any(instruction => instruction.OpCode.FlowControl == FlowControl.Branch
            || instruction.OpCode.FlowControl == FlowControl.Cond_Branch);
    }

    public string GetCorrelationId()
    {
        var list = GetPotentialCorrelationIds();

        if (list.Length > 1)
        {
            throw new ErrorsException("Saga can only have one correlation property identified by .ToSaga() expressions. Fix mappings in ConfigureHowToFindSaga to map to a single correlation property or decorate the saga with [SqlSaga] attribute.");
        }

        return list.FirstOrDefault();
    }

    string[] GetPotentialCorrelationIds()
    {
        var loadInstructionsOnSagaData = instructions
            .Where(instruction => instruction.OpCode.Code == Code.Ldtoken)
            .Select(instruction => new
            {
                Instruction = instruction,
                MethodDefinition = instruction.Operand as MethodDefinition
            })
            // Some Ldtokens have operands of type TypeDefinition, for loading types
            .Where(x => x.MethodDefinition != null)
            // We don't care about the ones where we're loading something from the message
            .Where(x => x.MethodDefinition.DeclaringType.FullName == sagaDataTypeName)
            .ToArray();

        if (loadInstructionsOnSagaData.Any(i => !i.MethodDefinition.Name.StartsWith("get_")))
        {
            // After having an expression on the saga data, we shouldn't be doing anything
            // except accessing properties
            throw new ErrorsException("ToSaga() expression in Saga's ConfigureHowToFindSaga method should point to a saga data property.");
        }

        var correlationList = loadInstructionsOnSagaData
            .Select(i => i.MethodDefinition.Name.Substring(4))
            .Distinct()
            .ToArray();

        return correlationList;
    }

    public bool CallsUnmanagedMethods()
    {
        // OpCode Calli is for calling into unmanaged code. Certainly don't need to be doing that inside
        // a ConfigureHowToFindSaga method: https://msdn.microsoft.com/en-us/library/d81ee808.aspx
        return instructions.Any(instruction => instruction.OpCode.Code == Code.Calli);
    }

    public bool CallsUnexpectedMethods()
    {
        var methodRefs = instructions
            .Where(instruction => instruction.OpCode.Code == Code.Call || instruction.OpCode.Code == Code.Callvirt)
            .Select(instruction => instruction.Operand as MethodReference)
            .ToArray();

        if (methodRefs.Any(methodRef => methodRef == null))
        {
            // Should not happen, all call/callvirt should have a MethodReference as an operand
            throw new Exception("Can't determine method call type for MSIL instruction");
        }

        var interestingCalls = methodRefs
            .Where(methodRef => !allowedMethods.Contains($"{methodRef.DeclaringType.FullName}::{methodRef.Name}"))
            .Select(methodRef => new MethodRefClassifier
            {
                MethodRef = methodRef,
                IsConfigureMapping = IsConfigureMappingMethodCall(methodRef),
                IsToSaga = IsToSagaMethodCall(methodRef),
                IsExpressionCall = IsExpressionCall(methodRef)
            })
            .Reverse()
            .ToArray();

        var expressionCallsAllowed = false;
        foreach (var item in interestingCalls)
        {
            if (item.IsConfigureMapping)
            {
                expressionCallsAllowed = true;
            }
            else if (item.IsToSaga)
            {
                expressionCallsAllowed = false;
            }

            item.ExpressionCallsAllowed = expressionCallsAllowed;
        }

        var badCalls = interestingCalls
            .Where(item => !item.IsConfigureMapping && !item.IsToSaga)
            .Where(item => !(item.IsExpressionCall && item.ExpressionCallsAllowed.Value))
            .ToArray();

        return badCalls.Length > 0;
    }

    class MethodRefClassifier
    {
        public MethodReference MethodRef { get; set; }
        public bool IsConfigureMapping { get; set; }
        public bool IsToSaga { get; set; }
        public bool IsExpressionCall { get; set; }
        public bool? ExpressionCallsAllowed { get; set; }
    }

    bool IsConfigureMappingMethodCall(MethodReference methodRef)
    {
        // FullName would be NServiceBus.SagaPropertyMapper<SagaData>
        return methodRef.Name == "ConfigureMapping" 
               && methodRef.DeclaringType.FullName.StartsWith("NServiceBus.SagaPropertyMapper`");
    }

    bool IsToSagaMethodCall(MethodReference methodRef)
    {
        // FullName would be NServiceBus.ToSagaExpression<SagaData,MessageType>
        // Don't validate the entire thing because we won't know the message type, and could be called multiple times
        return methodRef.Name == "ToSaga"
               && methodRef.DeclaringType.FullName.StartsWith("NServiceBus.ToSagaExpression`");
    }

    bool IsExpressionCall(MethodReference methodRef)
    {
        return methodRef.DeclaringType.FullName == "System.Linq.Expressions.Expression"
               && methodRef.Name == "Call";
    }

}
