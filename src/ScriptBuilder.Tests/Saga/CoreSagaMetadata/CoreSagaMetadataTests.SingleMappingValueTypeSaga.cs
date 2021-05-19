using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class SingleMappingValueTypeSaga : Saga<SingleMappingValueTypeSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public int Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }
}
/* IL:
SingleMappingValueTypeSaga.ConfigureHowToFindSaga:
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
IL_003A:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+SingleMappingValueTypeSaga+SagaData>.ConfigureMapping<MessageA>
IL_003F:  ldtoken     UserQuery+SingleMappingValueTypeSaga.SagaData
IL_0044:  call        System.Type.GetTypeFromHandle
IL_0049:  ldstr       "saga"
IL_004E:  call        System.Linq.Expressions.Expression.Parameter
IL_0053:  stloc.0
IL_0054:  ldloc.0
IL_0055:  ldtoken     UserQuery+SingleMappingValueTypeSaga+SagaData.get_Correlation
IL_005A:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_005F:  castclass   System.Reflection.MethodInfo
IL_0064:  call        System.Linq.Expressions.Expression.Property
IL_0069:  ldtoken     System.Object
IL_006E:  call        System.Type.GetTypeFromHandle
IL_0073:  call        System.Linq.Expressions.Expression.Convert
IL_0078:  ldc.i4.1
IL_0079:  newarr      System.Linq.Expressions.ParameterExpression
IL_007E:  dup
IL_007F:  ldc.i4.0
IL_0080:  ldloc.0
IL_0081:  stelem.ref
IL_0082:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0087:  callvirt    NServiceBus.ToSagaExpression<UserQuery+SingleMappingValueTypeSaga+SagaData,UserQuery+MessageA>.ToSaga
IL_008C:  ret
*/