using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ConcatMsgPropertiesSaga : Saga<ConcatMsgPropertiesSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageC>(msg => msg.Part1 + msg.Part2).ToSaga(saga => saga.Correlation);
        }
    }
}
/* IL:
ConcatMsgPropertiesSaga.ConfigureHowToFindSaga:
IL_0000:  ldarg.1
IL_0001:  ldtoken     UserQuery.MessageC
IL_0006:  call        System.Type.GetTypeFromHandle
IL_000B:  ldstr       "msg"
IL_0010:  call        System.Linq.Expressions.Expression.Parameter
IL_0015:  stloc.0
IL_0016:  ldloc.0
IL_0017:  ldtoken     UserQuery+MessageC.get_Part1
IL_001C:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0021:  castclass   System.Reflection.MethodInfo
IL_0026:  call        System.Linq.Expressions.Expression.Property
IL_002B:  ldloc.0
IL_002C:  ldtoken     UserQuery+MessageC.get_Part2
IL_0031:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0036:  castclass   System.Reflection.MethodInfo
IL_003B:  call        System.Linq.Expressions.Expression.Property
IL_0040:  ldtoken     System.String.Concat
IL_0045:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_004A:  castclass   System.Reflection.MethodInfo
IL_004F:  call        System.Linq.Expressions.Expression.Add
IL_0054:  ldc.i4.1
IL_0055:  newarr      System.Linq.Expressions.ParameterExpression
IL_005A:  dup
IL_005B:  ldc.i4.0
IL_005C:  ldloc.0
IL_005D:  stelem.ref
IL_005E:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0063:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+ConcatMsgPropertiesSaga+SagaData>.ConfigureMapping<MessageC>
IL_0068:  ldtoken     UserQuery+ConcatMsgPropertiesSaga.SagaData
IL_006D:  call        System.Type.GetTypeFromHandle
IL_0072:  ldstr       "saga"
IL_0077:  call        System.Linq.Expressions.Expression.Parameter
IL_007C:  stloc.0
IL_007D:  ldloc.0
IL_007E:  ldtoken     UserQuery+ConcatMsgPropertiesSaga+SagaData.get_Correlation
IL_0083:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0088:  castclass   System.Reflection.MethodInfo
IL_008D:  call        System.Linq.Expressions.Expression.Property
IL_0092:  ldc.i4.1
IL_0093:  newarr      System.Linq.Expressions.ParameterExpression
IL_0098:  dup
IL_0099:  ldc.i4.0
IL_009A:  ldloc.0
IL_009B:  stelem.ref
IL_009C:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_00A1:  callvirt    NServiceBus.ToSagaExpression<UserQuery+ConcatMsgPropertiesSaga+SagaData,UserQuery+MessageC>.ToSaga
IL_00A6:  ret
*/