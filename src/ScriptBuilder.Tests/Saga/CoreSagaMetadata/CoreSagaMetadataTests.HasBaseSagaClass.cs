using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class HasBaseSagaClass : BaseSaga<HasBaseSagaClass.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            base.ConfigureHowToFindSaga(mapper);
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }

    public class BaseSaga<TSaga> : Saga<TSaga>
        where TSaga : class, IContainSagaData, new()
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TSaga> mapper)
        {

        }
    }
}
/* IL:
HasBaseSagaClass.ConfigureHowToFindSaga:
IL_0000:  ldarg.0
IL_0001:  ldarg.1
IL_0002:  call        UserQuery+BaseSaga<UserQuery+HasBaseSagaClass+SagaData>.ConfigureHowToFindSaga
IL_0007:  ldarg.1
IL_0008:  ldtoken     UserQuery.MessageA
IL_000D:  call        System.Type.GetTypeFromHandle
IL_0012:  ldstr       "msg"
IL_0017:  call        System.Linq.Expressions.Expression.Parameter
IL_001C:  stloc.0
IL_001D:  ldloc.0
IL_001E:  ldtoken     UserQuery+MessageA.get_Correlation
IL_0023:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0028:  castclass   System.Reflection.MethodInfo
IL_002D:  call        System.Linq.Expressions.Expression.Property
IL_0032:  ldc.i4.1
IL_0033:  newarr      System.Linq.Expressions.ParameterExpression
IL_0038:  dup
IL_0039:  ldc.i4.0
IL_003A:  ldloc.0
IL_003B:  stelem.ref
IL_003C:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_0041:  callvirt    NServiceBus.SagaPropertyMapper<UserQuery+HasBaseSagaClass+SagaData>.ConfigureMapping<MessageA>
IL_0046:  ldtoken     UserQuery+HasBaseSagaClass.SagaData
IL_004B:  call        System.Type.GetTypeFromHandle
IL_0050:  ldstr       "saga"
IL_0055:  call        System.Linq.Expressions.Expression.Parameter
IL_005A:  stloc.0
IL_005B:  ldloc.0
IL_005C:  ldtoken     UserQuery+HasBaseSagaClass+SagaData.get_Correlation
IL_0061:  call        System.Reflection.MethodBase.GetMethodFromHandle
IL_0066:  castclass   System.Reflection.MethodInfo
IL_006B:  call        System.Linq.Expressions.Expression.Property
IL_0070:  ldc.i4.1
IL_0071:  newarr      System.Linq.Expressions.ParameterExpression
IL_0076:  dup
IL_0077:  ldc.i4.0
IL_0078:  ldloc.0
IL_0079:  stelem.ref
IL_007A:  call        System.Linq.Expressions.Expression.Lambda<Func`2>
IL_007F:  callvirt    NServiceBus.ToSagaExpression<UserQuery+HasBaseSagaClass+SagaData,UserQuery+MessageA>.ToSaga
IL_0084:  ret
*/