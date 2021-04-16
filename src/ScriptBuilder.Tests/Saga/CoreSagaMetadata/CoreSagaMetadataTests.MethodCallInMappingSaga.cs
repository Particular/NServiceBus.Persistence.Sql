using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class MethodCallInMappingSaga : Saga<MethodCallInMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => MapInMethod(saga));
        }

        static object MapInMethod(SagaData data)
        {
            return data.Correlation;
        }
    }
}
/* IL:
MethodCallInMappingSaga.ConfigureHowToFindSaga:
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
IL_003A:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+MethodCallInMappingSaga+SagaData>.ConfigureMapping<MessageA>
IL_003F:  ldtoken     UserQuery+MethodCallInMappingSaga.SagaData
IL_0044:  call        System.Type.GetTypeFromHandle
IL_0049:  ldstr       "saga"
IL_004E:  call        System.Linq.Expressions.Expression.Parameter
IL_0053:  stloc.0
IL_0054:  ldnull
IL_0055:  ldtoken     UserQuery+MethodCallInMappingSaga.MapInMethod
IL_005A:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_005F:  castclass   System.Reflection.MethodInfo
IL_0064:  ldc.i4.1
IL_0065:  newarr      System.Linq.Expressions.Expression
IL_006A:  dup
IL_006B:  ldc.i4.0
IL_006C:  ldloc.0
IL_006D:  stelem.ref
IL_006E:  call        System.Linq.Expressions.Expression.Call
IL_0073:  ldc.i4.1
IL_0074:  newarr      System.Linq.Expressions.ParameterExpression
IL_0079:  dup
IL_007A:  ldc.i4.0
IL_007B:  ldloc.0
IL_007C:  stelem.ref
IL_007D:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0082:  callvirt    NServiceBus.ToSagaExpression<UserQuery+MethodCallInMappingSaga+SagaData,UserQuery+MessageA>.ToSaga
IL_0087:  ret
*/