using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NServiceBus.Saga;

class SagaPropertyMapper : IConfigureHowToFindSagaWithMessage
{
    public List<string> Properties  = new List<string>();
    public void ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty) where TSagaEntity : IContainSagaData
    {
        Properties.Add(ExtractProperty(sagaEntityProperty));
    }

    internal static string ExtractProperty<TSagaEntity>(Expression<Func<TSagaEntity, object>> expression)
    {
        var member = expression.Body as MemberExpression;
        if (member == null)
        {
            throw new Exception("Not a MemberExpression");
        }
        return member.Member.Name;
    }
}