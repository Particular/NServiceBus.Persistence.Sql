using System;
using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class DelegateCallingSaga : Saga<DelegateCallingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            Action action = () => mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            action();
        }
    }
}
/* IL:
DelegateCallingSaga.ConfigureHowToFindSaga:
IL_0000:  newobj      UserQuery+DelegateCallingSaga+<>c__DisplayClass1_0..ctor
IL_0005:  dup
IL_0006:  ldarg.1
IL_0007:  stfld       UserQuery+DelegateCallingSaga+<>c__DisplayClass1_0.mapper
IL_000C:  ldftn       UserQuery+DelegateCallingSaga+<>c__DisplayClass1_0.<ConfigureHowToFindSaga>b__0
IL_0012:  newobj      System.Action..ctor
IL_0017:  callvirt    System.Action.Invoke
IL_001C:  ret
*/