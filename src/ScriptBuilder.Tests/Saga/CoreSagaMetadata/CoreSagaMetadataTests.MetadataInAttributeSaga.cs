using NServiceBus;
using NServiceBus.Persistence.Sql;

public partial class CoreSagaMetadataTests
{
    [SqlSaga(transitionalCorrelationProperty: "TransitionalCorrId", tableSuffix: "DifferentTableSuffix")]
    public class MetadataInAttributeSaga : Saga<MetadataInAttributeSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string TransitionalCorrId { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }
}
/* IL:
MetadataInAttributeSaga.ConfigureHowToFindSaga:
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
IL_003A:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+MetadataInAttributeSaga+SagaData>.ConfigureMapping<MessageA>
IL_003F:  ldtoken     UserQuery+MetadataInAttributeSaga.SagaData
IL_0044:  call        System.Type.GetTypeFromHandle
IL_0049:  ldstr       "saga"
IL_004E:  call        System.Linq.Expressions.Expression.Parameter
IL_0053:  stloc.0
IL_0054:  ldloc.0
IL_0055:  ldtoken     UserQuery+MetadataInAttributeSaga+SagaData.get_Correlation
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
IL_0078:  callvirt    NServiceBus.ToSagaExpression<UserQuery+MetadataInAttributeSaga+SagaData,UserQuery+MessageA>.ToSaga
IL_007D:  ret
*/