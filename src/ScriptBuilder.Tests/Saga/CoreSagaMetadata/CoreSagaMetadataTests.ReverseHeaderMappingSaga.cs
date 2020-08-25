using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ReverseHeaderMappingSaga : Saga<ReverseHeaderMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string SagaCorrelation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.MapSaga(saga => saga.SagaCorrelation)
                .ToMessageHeader<MessageA>("SomeHeaderName");
        }
    }
}

/* IL:
ReverseHeaderMappingSaga.ConfigureHowToFindSaga:
IL_0000:  nop         
IL_0001:  ldarg.1     
IL_0002:  ldtoken     UserQuery+ReverseHeaderMappingSaga.SagaData
IL_0007:  call        System.Type.GetTypeFromHandle
IL_000C:  ldstr       "saga"
IL_0011:  call        System.Linq.Expressions.Expression.Parameter
IL_0016:  stloc.0     
IL_0017:  ldloc.0     
IL_0018:  ldtoken     UserQuery+ReverseHeaderMappingSaga+SagaData.get_SagaCorrelation
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
IL_003B:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+ReverseHeaderMappingSaga+SagaData>.MapSaga
IL_0040:  ldstr       "SomeHeaderName"
IL_0045:  callvirt    NServiceBus.CorrelatedSagaPropertyMapper<UserQuery+ReverseHeaderMappingSaga+SagaData>.ToMessageHeader<MessageA>
IL_004A:  pop         
IL_004B:  ret         
*/