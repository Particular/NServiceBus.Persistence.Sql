using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ConflictingCorrelationSaga : Saga<ConflictingCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string OtherProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            mapper.ConfigureMapping<MessageB>(msg => msg.Correlation).ToSaga(saga => saga.OtherProperty);
        }
    }
}
/* IL:
ConflictingCorrelationSaga.ConfigureHowToFindSaga:
IL_0000:  ldarg.1
IL_0001:  ldtoken     UserQuery.MessageA
IL_0006:  call        System.Type.GetTypeFromHandle
IL_000B:  ldstr       "msg"
IL_0010:  call        System.Linq.Expressions.Expression.Parameter
IL_0015:  stloc.0
IL_0016:  ldloc.0
IL_0017:  ldtoken     UserQuery+MessageA.get_Correlation
IL_001C:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0021:  castclass   System.Reflection.MethodInfo
IL_0026:  call        System.Linq.Expressions.Expression.Property
IL_002B:  ldc.i4.1
IL_002C:  newarr      System.Linq.Expressions.ParameterExpression
IL_0031:  dup
IL_0032:  ldc.i4.0
IL_0033:  ldloc.0
IL_0034:  stelem.ref
IL_0035:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_003A:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+ConflictingCorrelationSaga+SagaData>.ConfigureMapping<MessageA>
IL_003F:  ldtoken     UserQuery+ConflictingCorrelationSaga.SagaData
IL_0044:  call        System.Type.GetTypeFromHandle
IL_0049:  ldstr       "saga"
IL_004E:  call        System.Linq.Expressions.Expression.Parameter
IL_0053:  stloc.0
IL_0054:  ldloc.0
IL_0055:  ldtoken     UserQuery+ConflictingCorrelationSaga+SagaData.get_Correlation
IL_005A:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_005F:  castclass   System.Reflection.MethodInfo
IL_0064:  call        System.Linq.Expressions.Expression.Property
IL_0069:  ldc.i4.1
IL_006A:  newarr      System.Linq.Expressions.ParameterExpression
IL_006F:  dup
IL_0070:  ldc.i4.0
IL_0071:  ldloc.0
IL_0072:  stelem.ref
IL_0073:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0078:  callvirt    NServiceBus.ToSagaExpression<UserQuery+ConflictingCorrelationSaga+SagaData,UserQuery+MessageA>.ToSaga
IL_007D:  ldarg.1
IL_007E:  ldtoken     UserQuery.MessageB
IL_0083:  call        System.Type.GetTypeFromHandle
IL_0088:  ldstr       "msg"
IL_008D:  call        System.Linq.Expressions.Expression.Parameter
IL_0092:  stloc.0
IL_0093:  ldloc.0
IL_0094:  ldtoken     UserQuery+MessageB.get_Correlation
IL_0099:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_009E:  castclass   System.Reflection.MethodInfo
IL_00A3:  call        System.Linq.Expressions.Expression.Property
IL_00A8:  ldc.i4.1
IL_00A9:  newarr      System.Linq.Expressions.ParameterExpression
IL_00AE:  dup
IL_00AF:  ldc.i4.0
IL_00B0:  ldloc.0
IL_00B1:  stelem.ref
IL_00B2:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_00B7:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+ConflictingCorrelationSaga+SagaData>.ConfigureMapping<MessageB>
IL_00BC:  ldtoken     UserQuery+ConflictingCorrelationSaga.SagaData
IL_00C1:  call        System.Type.GetTypeFromHandle
IL_00C6:  ldstr       "saga"
IL_00CB:  call        System.Linq.Expressions.Expression.Parameter
IL_00D0:  stloc.0
IL_00D1:  ldloc.0
IL_00D2:  ldtoken     UserQuery+ConflictingCorrelationSaga+SagaData.get_OtherProperty
IL_00D7:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_00DC:  castclass   System.Reflection.MethodInfo
IL_00E1:  call        System.Linq.Expressions.Expression.Property
IL_00E6:  ldc.i4.1
IL_00E7:  newarr      System.Linq.Expressions.ParameterExpression
IL_00EC:  dup
IL_00ED:  ldc.i4.0
IL_00EE:  ldloc.0
IL_00EF:  stelem.ref
IL_00F0:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_00F5:  callvirt    NServiceBus.ToSagaExpression<UserQuery+ConflictingCorrelationSaga+SagaData,UserQuery+MessageB>.ToSaga
IL_00FA:  ret
*/