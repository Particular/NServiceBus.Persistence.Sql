using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class SwitchingLogicSaga : Saga<SwitchingLogicSaga.SagaData>
    {
        int number = 0;

        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string OtherProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            if (number > 0)
            {
                mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            }
            else
            {
                mapper.ConfigureMapping<MessageB>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            }
        }
    }
}
/* IL:
SwitchingLogicSaga.ConfigureHowToFindSaga:
IL_0000:  ldarg.0
IL_0001:  ldfld       UserQuery+SwitchingLogicSaga.number
IL_0006:  ldc.i4.0
IL_0007:  ble.s       IL_0087
IL_0009:  ldarg.1
IL_000A:  ldtoken     UserQuery.MessageA
IL_000F:  call        System.Type.GetTypeFromHandle
IL_0014:  ldstr       "msg"
IL_0019:  call        System.Linq.Expressions.Expression.Parameter
IL_001E:  stloc.0
IL_001F:  ldloc.0
IL_0020:  ldtoken     UserQuery+MessageA.get_Correlation
IL_0025:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_002A:  castclass   System.Reflection.MethodInfo
IL_002F:  call        System.Linq.Expressions.Expression.Property
IL_0034:  ldc.i4.1
IL_0035:  newarr      System.Linq.Expressions.ParameterExpression
IL_003A:  dup
IL_003B:  ldc.i4.0
IL_003C:  ldloc.0
IL_003D:  stelem.ref
IL_003E:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0043:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+SwitchingLogicSaga+SagaData>.ConfigureMapping<MessageA>
IL_0048:  ldtoken     UserQuery+SwitchingLogicSaga.SagaData
IL_004D:  call        System.Type.GetTypeFromHandle
IL_0052:  ldstr       "saga"
IL_0057:  call        System.Linq.Expressions.Expression.Parameter
IL_005C:  stloc.0
IL_005D:  ldloc.0
IL_005E:  ldtoken     UserQuery+SwitchingLogicSaga+SagaData.get_Correlation
IL_0063:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0068:  castclass   System.Reflection.MethodInfo
IL_006D:  call        System.Linq.Expressions.Expression.Property
IL_0072:  ldc.i4.1
IL_0073:  newarr      System.Linq.Expressions.ParameterExpression
IL_0078:  dup
IL_0079:  ldc.i4.0
IL_007A:  ldloc.0
IL_007B:  stelem.ref
IL_007C:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0081:  callvirt    NServiceBus.ToSagaExpression<UserQuery+SwitchingLogicSaga+SagaData,UserQuery+MessageA>.ToSaga
IL_0086:  ret
IL_0087:  ldarg.1
IL_0088:  ldtoken     UserQuery.MessageB
IL_008D:  call        System.Type.GetTypeFromHandle
IL_0092:  ldstr       "msg"
IL_0097:  call        System.Linq.Expressions.Expression.Parameter
IL_009C:  stloc.0
IL_009D:  ldloc.0
IL_009E:  ldtoken     UserQuery+MessageB.get_Correlation
IL_00A3:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_00A8:  castclass   System.Reflection.MethodInfo
IL_00AD:  call        System.Linq.Expressions.Expression.Property
IL_00B2:  ldc.i4.1
IL_00B3:  newarr      System.Linq.Expressions.ParameterExpression
IL_00B8:  dup
IL_00B9:  ldc.i4.0
IL_00BA:  ldloc.0
IL_00BB:  stelem.ref
IL_00BC:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_00C1:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+SwitchingLogicSaga+SagaData>.ConfigureMapping<MessageB>
IL_00C6:  ldtoken     UserQuery+SwitchingLogicSaga.SagaData
IL_00CB:  call        System.Type.GetTypeFromHandle
IL_00D0:  ldstr       "saga"
IL_00D5:  call        System.Linq.Expressions.Expression.Parameter
IL_00DA:  stloc.0
IL_00DB:  ldloc.0
IL_00DC:  ldtoken     UserQuery+SwitchingLogicSaga+SagaData.get_Correlation
IL_00E1:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_00E6:  castclass   System.Reflection.MethodInfo
IL_00EB:  call        System.Linq.Expressions.Expression.Property
IL_00F0:  ldc.i4.1
IL_00F1:  newarr      System.Linq.Expressions.ParameterExpression
IL_00F6:  dup
IL_00F7:  ldc.i4.0
IL_00F8:  ldloc.0
IL_00F9:  stelem.ref
IL_00FA:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_00FF:  callvirt    NServiceBus.ToSagaExpression<UserQuery+SwitchingLogicSaga+SagaData,UserQuery+MessageB>.ToSaga
IL_0104:  ret
*/