#pragma warning disable 1591
namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Linq.Expressions;

    [ObsoleteEx(
        RemoveInVersion = "3.0",
        TreatAsErrorFromVersion = "2.0",
        ReplacementTypeOrMember = nameof(IMessagePropertyMapper))]
    public class MessagePropertyMapper<SagaData>
    {
        [ObsoleteEx(
            RemoveInVersion = "3.0",
            TreatAsErrorFromVersion = "2.0",
            ReplacementTypeOrMember = nameof(IMessagePropertyMapper) + "." + nameof(IMessagePropertyMapper.ConfigureMapping))]
        public void MapMessage<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
        }
    }

    [ObsoleteEx(
        RemoveInVersion = "3.0",
        TreatAsErrorFromVersion = "2.0",
        ReplacementTypeOrMember = nameof(IMessagePropertyMapper))]
    public static class MessagePropertyMapper
    {
        [ObsoleteEx(
            RemoveInVersion = "3.0",
            TreatAsErrorFromVersion = "2.0",
            ReplacementTypeOrMember = nameof(IMessagePropertyMapper) + "." + nameof(IMessagePropertyMapper.ConfigureMapping))]
        public static void MapMessage<TMessage>(this IMessagePropertyMapper mapper, Expression<Func<TMessage, object>> messageProperty)
        {
        }
    }

}