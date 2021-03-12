using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ConcatMsgPropertiesWithInterpolationSaga : Saga<ConcatMsgPropertiesWithInterpolationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageC>(msg => $"{msg.Part1}{msg.Part2}").ToSaga(saga => saga.Correlation);
        }
    }
}
/* IL:
ConcatMsgPropertiesWithInterpolationSaga.ConfigureHowToFindSaga:
IL_0000:  ldarg.1
IL_0001:  ldtoken     UserQuery.MessageC
IL_0006:  call        System.Type.GetTypeFromHandle
IL_000B:  ldstr       "msg"
IL_0010:  call        System.Linq.Expressions.Expression.Parameter
IL_0015:  stloc.0
IL_0016:  ldnull
IL_0017:  ldtoken     System.String.Format
IL_001C:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0021:  castclass   System.Reflection.MethodInfo
IL_0026:  ldc.i4.3
IL_0027:  newarr      System.Linq.Expressions.Expression
IL_002C:  dup
IL_002D:  ldc.i4.0
IL_002E:  ldstr       "{0}{1}"
IL_0033:  ldtoken     System.String
IL_0038:  call        System.Type.GetTypeFromHandle
IL_003D:  call        System.Linq.Expressions.Expression.Constant
IL_0042:  stelem.ref
IL_0043:  dup
IL_0044:  ldc.i4.1
IL_0045:  ldloc.0
IL_0046:  ldtoken     UserQuery+MessageC.get_Part1
IL_004B:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0050:  castclass   System.Reflection.MethodInfo
IL_0055:  call        System.Linq.Expressions.Expression.Property
IL_005A:  stelem.ref
IL_005B:  dup
IL_005C:  ldc.i4.2
IL_005D:  ldloc.0
IL_005E:  ldtoken     UserQuery+MessageC.get_Part2
IL_0063:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0068:  castclass   System.Reflection.MethodInfo
IL_006D:  call        System.Linq.Expressions.Expression.Property
IL_0072:  stelem.ref
IL_0073:  call        System.Linq.Expressions.Expression.Call
IL_0078:  ldc.i4.1
IL_0079:  newarr      System.Linq.Expressions.ParameterExpression
IL_007E:  dup
IL_007F:  ldc.i4.0
IL_0080:  ldloc.0
IL_0081:  stelem.ref
IL_0082:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0087:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+ConcatMsgPropertiesWithInterpolationSaga+SagaData>.ConfigureMapping<MessageC>
IL_008C:  ldtoken     UserQuery+ConcatMsgPropertiesWithInterpolationSaga.SagaData
IL_0091:  call        System.Type.GetTypeFromHandle
IL_0096:  ldstr       "saga"
IL_009B:  call        System.Linq.Expressions.Expression.Parameter
IL_00A0:  stloc.0
IL_00A1:  ldloc.0
IL_00A2:  ldtoken     UserQuery+ConcatMsgPropertiesWithInterpolationSaga+SagaData.get_Correlation
IL_00A7:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_00AC:  castclass   System.Reflection.MethodInfo
IL_00B1:  call        System.Linq.Expressions.Expression.Property
IL_00B6:  ldc.i4.1
IL_00B7:  newarr      System.Linq.Expressions.ParameterExpression
IL_00BC:  dup
IL_00BD:  ldc.i4.0
IL_00BE:  ldloc.0
IL_00BF:  stelem.ref
IL_00C0:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_00C5:  callvirt    NServiceBus.ToSagaExpression<UserQuery+ConcatMsgPropertiesWithInterpolationSaga+SagaData,UserQuery+MessageC>.ToSaga
IL_00CA:  ret
*/