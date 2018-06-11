using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using NServiceBus.Persistence.Sql;

class CoreSagaCorrelationPropertyReader
{
    string sagaDataTypeName;
    Collection<Instruction> instructions;
    ILookup<Code, Instruction> instructionsLookup;

    public CoreSagaCorrelationPropertyReader(TypeDefinition type, TypeDefinition sagaDataType)
    {
        sagaDataTypeName = sagaDataType.FullName;
        var configureMethod = type.Methods.FirstOrDefault(m => m.Name == "ConfigureHowToFindSaga");
        if (configureMethod == null)
        {
            throw new ErrorsException("Saga does not contain a ConfigureHowToFindSaga method.");
        }

        this.instructions = configureMethod.Body.Instructions;
        this.instructionsLookup = instructions.ToLookup(instruction => instruction.OpCode.Code);
    }

    public bool ContainsBranchingLogic()
    {
        return instructions.Any(instruction => instruction.OpCode.FlowControl == FlowControl.Branch
            || instruction.OpCode.FlowControl == FlowControl.Cond_Branch);
    }

}
