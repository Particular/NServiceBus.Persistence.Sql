using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ReverseMappingSaga : Saga<ReverseMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string SagaCorrelation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.MapSaga(saga => saga.SagaCorrelation)
                .ToMessage<MessageA>(msg => msg.Correlation);
        }
    }
}
/* IL:
ReverseMappingSaga.ConfigureHowToFindSaga:
IL_0000:  nop         
IL_0001:  ldarg.1     
IL_0002:  ldtoken     UserQuery+ReverseMappingSaga.SagaData
IL_0007:  call        System.Type.GetTypeFromHandle
IL_000C:  ldstr       "saga"
IL_0011:  call        System.Linq.Expressions.Expression.Parameter
IL_0016:  stloc.0     
IL_0017:  ldloc.0     
IL_0018:  ldtoken     UserQuery+ReverseMappingSaga+SagaData.get_Correlation
IL_001D:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0022:  castclass   System.Reflection.MethodInfo
IL_0027:  call        System.Linq.Expressions.Expression.Property
IL_002C:  ldc.i4.1    
IL_002D:  newarr      System.Linq.Expressions.ParameterExpression
IL_0032:  dup         
IL_0033:  ldc.i4.0    
IL_0034:  ldloc.0     
IL_0035:  stelem.ref  
IL_0036:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_003B:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+ReverseMappingSaga+SagaData>.MapSaga
IL_0040:  ldtoken     UserQuery.MessageA
IL_0045:  call        System.Type.GetTypeFromHandle
IL_004A:  ldstr       "msg"
IL_004F:  call        System.Linq.Expressions.Expression.Parameter
IL_0054:  stloc.0     
IL_0055:  ldloc.0     
IL_0056:  ldtoken     UserQuery+MessageA.get_Correlation
IL_005B:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0060:  castclass   System.Reflection.MethodInfo
IL_0065:  call        System.Linq.Expressions.Expression.Property
IL_006A:  ldc.i4.1    
IL_006B:  newarr      System.Linq.Expressions.ParameterExpression
IL_0070:  dup         
IL_0071:  ldc.i4.0    
IL_0072:  ldloc.0     
IL_0073:  stelem.ref  
IL_0074:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0079:  callvirt    NServiceBus.CorrelatedSagaPropertyMapper<UserQuery+ReverseMappingSaga+SagaData>.ToMessage<MessageA>
IL_007E:  pop         
IL_007F:  ret         
*/