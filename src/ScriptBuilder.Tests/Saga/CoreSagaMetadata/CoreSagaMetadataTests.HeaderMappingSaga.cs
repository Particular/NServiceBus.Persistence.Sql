using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class HeaderMappingSaga : Saga<HeaderMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string SagaCorrelation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureHeaderMapping<MessageA>("SomeHeaderName")
                .ToSaga(saga => saga.SagaCorrelation);
        }
    }
}

/* IL:
HeaderMappingSaga.ConfigureHowToFindSaga:
IL_0000:  nop         
IL_0001:  ldarg.1     
IL_0002:  ldstr       "SomeHeaderName"
IL_0007:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+HeaderMappingSaga+SagaData>.ConfigureHeaderMapping<MessageA>
IL_000C:  ldtoken     UserQuery+HeaderMappingSaga.SagaData
IL_0011:  call        System.Type.GetTypeFromHandle
IL_0016:  ldstr       "saga"
IL_001B:  call        System.Linq.Expressions.Expression.Parameter
IL_0020:  stloc.0     
IL_0021:  ldloc.0     
IL_0022:  ldtoken     UserQuery+HeaderMappingSaga+SagaData.get_SagaCorrelation
IL_0027:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_002C:  castclass   System.Reflection.MethodInfo
IL_0031:  call        System.Linq.Expressions.Expression.Property
IL_0036:  ldc.i4.1    
IL_0037:  newarr      System.Linq.Expressions.ParameterExpression
IL_003C:  dup         
IL_003D:  ldc.i4.0    
IL_003E:  ldloc.0     
IL_003F:  stelem.ref  
IL_0040:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0045:  callvirt    NServiceBus.IToSagaExpression<UserQuery+HeaderMappingSaga+SagaData>.ToSaga
IL_004A:  nop         
IL_004B:  ret         
*/