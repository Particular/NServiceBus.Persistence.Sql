using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using NServiceBus.Persistence.Sql;

class CoreSagaCorrelationPropertyReader
{
    string sagaDataTypeName;
    Collection<Instruction> instructions;

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

    public string[] GetPotentialCorrelationIds()
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

    public string GetCorrelationId()
    {
        var list = GetPotentialCorrelationIds();

        if (list.Length > 1)
        {
            throw new ErrorsException("Saga can only have one correlation property identified by .ToSaga() expressions. Fix mappings in ConfigureHowToFindSaga to map to a single correlation property or decorate the saga with [SqlSaga] attribute.");
        }

        return list.FirstOrDefault();
    }

}
