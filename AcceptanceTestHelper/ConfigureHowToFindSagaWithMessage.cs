using System;
using System.Linq.Expressions;
using System.Reflection;
using NServiceBus;

class ConfigureHowToFindSagaWithMessage : IConfigureHowToFindSagaWithMessage
{
    public void ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty) where TSagaEntity : IContainSagaData
    {
        var body = sagaEntityProperty.Body;
        var member = GetMemberExpression(body);
        var property = (PropertyInfo)member.Member;
        CorrelationProperty = property.Name;
        CorrelationType = property.PropertyType;
    }

    MemberExpression GetMemberExpression(Expression body)
    {
        var unaryExpression = body as UnaryExpression;
        if (unaryExpression != null)
        {
            return (MemberExpression)unaryExpression.Operand;
        }
        var memberExpression = body as MemberExpression;
        if (memberExpression != null)
        {
            return memberExpression;
        }
        throw new Exception(body.GetType().FullName);
    }

    public string CorrelationProperty;
    public Type CorrelationType;
}