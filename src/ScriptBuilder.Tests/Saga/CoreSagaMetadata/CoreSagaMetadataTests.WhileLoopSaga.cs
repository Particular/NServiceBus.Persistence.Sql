using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class WhileLoopSaga : Saga<WhileLoopSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            var i = 0;
            while (i < 3)
            {
                mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
                i++;
            }
        }
    }
}
/* IL:  
WhileLoopSaga.ConfigureHowToFindSaga:
IL_0000:  ldc.i4.0    
IL_0001:  stloc.0     // i
IL_0002:  br          IL_0088
IL_0007:  ldarg.1     
IL_0008:  ldtoken     UserQuery.MessageA
IL_000D:  call        System.Type.GetTypeFromHandle
IL_0012:  ldstr       "msg"
IL_0017:  call        System.Linq.Expressions.Expression.Parameter
IL_001C:  stloc.1     
IL_001D:  ldloc.1     
IL_001E:  ldtoken     UserQuery+MessageA.get_Correlation
IL_0023:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0028:  castclass   System.Reflection.MethodInfo
IL_002D:  call        System.Linq.Expressions.Expression.Property
IL_0032:  ldc.i4.1    
IL_0033:  newarr      System.Linq.Expressions.ParameterExpression
IL_0038:  dup         
IL_0039:  ldc.i4.0    
IL_003A:  ldloc.1     
IL_003B:  stelem.ref  
IL_003C:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0041:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+WhileLoopSaga+SagaData>.ConfigureMapping<MessageA>
IL_0046:  ldtoken     UserQuery+WhileLoopSaga.SagaData
IL_004B:  call        System.Type.GetTypeFromHandle
IL_0050:  ldstr       "saga"
IL_0055:  call        System.Linq.Expressions.Expression.Parameter
IL_005A:  stloc.1     
IL_005B:  ldloc.1     
IL_005C:  ldtoken     UserQuery+WhileLoopSaga+SagaData.get_Correlation
IL_0061:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0066:  castclass   System.Reflection.MethodInfo
IL_006B:  call        System.Linq.Expressions.Expression.Property
IL_0070:  ldc.i4.1    
IL_0071:  newarr      System.Linq.Expressions.ParameterExpression
IL_0076:  dup         
IL_0077:  ldc.i4.0    
IL_0078:  ldloc.1     
IL_0079:  stelem.ref  
IL_007A:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_007F:  callvirt    NServiceBus.ToSagaExpression<UserQuery+WhileLoopSaga+SagaData,UserQuery+MessageA>.ToSaga
IL_0084:  ldloc.0     // i
IL_0085:  ldc.i4.1    
IL_0086:  add         
IL_0087:  stloc.0     // i
IL_0088:  ldloc.0     // i
IL_0089:  ldc.i4.3    
IL_008A:  blt         IL_0007
IL_008F:  ret
*/
