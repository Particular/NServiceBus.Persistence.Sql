using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ConcatMsgPropertiesWithOtherMethods : Saga<ConcatMsgPropertiesWithOtherMethods.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageC>(msg => msg.Part1.ToUpper() + msg.Part2.ToLowerInvariant())
                .ToSaga(saga => saga.Correlation);
        }
    }
}
/* IL:
ConcatMsgPropertiesWithOtherMethods.ConfigureHowToFindSaga:
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
IL_002B:  ldtoken     System.String.ToUpper
IL_0030:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0035:  castclass   System.Reflection.MethodInfo
IL_003A:  call        System.Array.Empty<Expression>
IL_003F:  call        System.Linq.Expressions.Expression.Call
IL_0044:  ldloc.0     
IL_0045:  ldtoken     UserQuery+MessageC.get_Part2
IL_004A:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_004F:  castclass   System.Reflection.MethodInfo
IL_0054:  call        System.Linq.Expressions.Expression.Property
IL_0059:  ldtoken     System.String.ToLowerInvariant
IL_005E:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0063:  castclass   System.Reflection.MethodInfo
IL_0068:  call        System.Array.Empty<Expression>
IL_006D:  call        System.Linq.Expressions.Expression.Call
IL_0072:  ldtoken     System.String.Concat
IL_0077:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_007C:  castclass   System.Reflection.MethodInfo
IL_0081:  call        System.Linq.Expressions.Expression.Add
IL_0086:  ldc.i4.1    
IL_0087:  newarr      System.Linq.Expressions.ParameterExpression
IL_008C:  dup         
IL_008D:  ldc.i4.0    
IL_008E:  ldloc.0     
IL_008F:  stelem.ref  
IL_0090:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0095:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+ConcatMsgPropertiesWithOtherMethods+SagaData>.ConfigureMapping<MessageC>
IL_009A:  ldtoken     UserQuery+ConcatMsgPropertiesWithOtherMethods.SagaData
IL_009F:  call        System.Type.GetTypeFromHandle
IL_00A4:  ldstr       "saga"
IL_00A9:  call        System.Linq.Expressions.Expression.Parameter
IL_00AE:  stloc.0     
IL_00AF:  ldloc.0     
IL_00B0:  ldtoken     UserQuery+ConcatMsgPropertiesWithOtherMethods+SagaData.get_Correlation
IL_00B5:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_00BA:  castclass   System.Reflection.MethodInfo
IL_00BF:  call        System.Linq.Expressions.Expression.Property
IL_00C4:  ldc.i4.1    
IL_00C5:  newarr      System.Linq.Expressions.ParameterExpression
IL_00CA:  dup         
IL_00CB:  ldc.i4.0    
IL_00CC:  ldloc.0     
IL_00CD:  stelem.ref  
IL_00CE:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_00D3:  callvirt    NServiceBus.ToSagaExpression<UserQuery+ConcatMsgPropertiesWithOtherMethods+SagaData,UserQuery+MessageC>.ToSaga
IL_00D8:  ret  
*/
