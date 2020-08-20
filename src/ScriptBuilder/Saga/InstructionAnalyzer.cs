using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NServiceBus.Persistence.Sql;

static class InstructionAnalyzer
{
    static HashSet<string> allowedMethods;

    static InstructionAnalyzer()
    {
        allowedMethods = new HashSet<string>(new[]
        {
            "System.Array::Empty",
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

    public static IList<Instruction> GetConfigureHowToFindSagaInstructions(TypeDefinition sagaTypeDefinition)
    {
        var configureMethod = sagaTypeDefinition.Methods.FirstOrDefault(m => m.Name == "ConfigureHowToFindSaga");
        if (configureMethod == null)
        {
            throw new ErrorsException("Saga does not contain a ConfigureHowToFindSaga method.");
        }

        return configureMethod.Body.Instructions;
    }

    public static bool ContainsBranchingLogic(IList<Instruction> instructions)
    {
        return instructions.Any(instruction => instruction.OpCode.FlowControl == FlowControl.Branch
            || instruction.OpCode.FlowControl == FlowControl.Cond_Branch);
    }

    public static string GetCorrelationId(IList<Instruction> instructions, string sagaDataTypeName)
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

        if (correlationList.Length > 1)
        {
            throw new ErrorsException("Saga can only have one correlation property identified by .ToSaga() expressions. Fix mappings in ConfigureHowToFindSaga to map to a single correlation property or decorate the saga with [SqlSaga] attribute.");
        }

        return correlationList.FirstOrDefault();
    }

    public static bool CallsUnmanagedMethods(IList<Instruction> instructions)
    {
        // OpCode Calli is for calling into unmanaged code. Certainly don't need to be doing that inside
        // a ConfigureHowToFindSaga method: https://msdn.microsoft.com/en-us/library/d81ee808.aspx
        return instructions.Any(instruction => instruction.OpCode.Code == Code.Calli);
    }

    public static bool CallsUnexpectedMethods(IList<Instruction> instructions)
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
                IsConfigureMapping = IsMessageMapping(methodRef),
                IsToSaga = IsSagaMapping(methodRef),
                IsExpressionCall = IsExpressionCall(methodRef)
            })
            .Reverse()  // <----- Note on reversal below
            .ToArray();

        /* The ability to do Call expressions is necessary on the message mapping side in order to be able to,
         * for instance, concatenate 2 message properties together into one value to look up in the saga data,
         * i.e. ConfigureMapping(msg => $"{msg.PropA}/{msg.PropB}), and honestly in the message mapping portion
         * we don't really care. But in the saga data portion, we want to be more conservative. Because of the way
         * IL works, you have to get your arguments set up before you invoke the method with the pointers to
         * arguments stored in registers, so that means everything related to ConfigureMapping comes BEFORE that call,
         * and everything relating to ToSaga (where the correlation id will come from) comes before THAT call. By
         * reversing the order of the methods that are left, we can see ConfigureMapping and ToSaga as signposts to
         * determine whether the upcoming calls are either allowed or not.
         */

        var expressionCallsAllowed = false;
        foreach (var item in interestingCalls)
        {
            if (item.IsConfigureMapping)
            {
                // This expression call (and those after) came before ConfigureMapping, meaning it's an argument of ConfigureMapping. It's OK.
                expressionCallsAllowed = true;
            }
            else if (item.IsToSaga)
            {
                // This expression call (and those after) came before ToSaga, meaning it's an argument of ToSaga. Don't allow it.
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

    static bool IsMessageMapping(MethodReference methodRef) =>
        IsConfigureMappingMethodCall(methodRef) 
        || IsConfigureHeaderMapping(methodRef) 
        || IsToMessageMethodCall(methodRef) 
        || IsToMessageHeaderMethodCall(methodRef);

    static bool IsSagaMapping(MethodReference methodRef) => 
        IsToSagaMethodCall(methodRef) 
        || IsMapSagaMappingMethodCall(methodRef);

    static bool IsConfigureHeaderMapping(MethodReference methodRef)
    {
        // FullName would be NServiceBus.SagaPropertyMapper<SagaData>
        return methodRef.Name == "ConfigureHeaderMapping" 
               && methodRef.DeclaringType.FullName.StartsWith("NServiceBus.SagaPropertyMapper`");
    }

    static bool IsConfigureMappingMethodCall(MethodReference methodRef)
    {
        // FullName would be NServiceBus.SagaPropertyMapper<SagaData>
        return methodRef.Name == "ConfigureMapping" 
               && methodRef.DeclaringType.FullName.StartsWith("NServiceBus.SagaPropertyMapper`");
    }

    static bool IsMapSagaMappingMethodCall(MethodReference methodRef)
    {
        // FullName would be NServiceBus.SagaPropertyMapper<SagaData>
        return methodRef.Name == "MapSaga" 
               && methodRef.DeclaringType.FullName.StartsWith("NServiceBus.SagaPropertyMapper`");
    }

    static bool IsToSagaMethodCall(MethodReference methodRef)
    {
        // FullName would be NServiceBus.ToSagaExpression<SagaData,MessageType>
        // Don't validate the entire thing because we won't know the message type, and could be called multiple times
        var sagaExpression = "NServiceBus.ToSagaExpression`";
        var sagaExpressionInterface = "NServiceBus.IToSagaExpression`";
            
        return methodRef.Name == "ToSaga"
               && (methodRef.DeclaringType.FullName.StartsWith(sagaExpression) || methodRef.DeclaringType.FullName.StartsWith(sagaExpressionInterface));
    }

    static bool IsToMessageMethodCall(MethodReference methodRef)
    {
        // FullName would be NServiceBus.CorrelatedSagaPropertyMapper<SagaData>
        // Don't validate the entire thing because we won't know the message type, and could be called multiple times
        var expression = "NServiceBus.CorrelatedSagaPropertyMapper`";

        return methodRef.Name == "ToMessage"
               && methodRef.DeclaringType.FullName.StartsWith(expression);
    }

    static bool IsToMessageHeaderMethodCall(MethodReference methodRef)
    {
        // FullName would be NServiceBus.CorrelatedSagaPropertyMapper<SagaData>
        // Don't validate the entire thing because we won't know the message type, and could be called multiple times
        var expression = "NServiceBus.CorrelatedSagaPropertyMapper`";

        return methodRef.Name == "ToMessageHeader"
               && methodRef.DeclaringType.FullName.StartsWith(expression);
    }

    static bool IsExpressionCall(MethodReference methodRef)
    {
        return methodRef.DeclaringType.FullName == "System.Linq.Expressions.Expression"
               && methodRef.Name == "Call";
    }

}
