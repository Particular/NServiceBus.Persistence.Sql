using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using NServiceBus.Saga;

class SagaPropertyMapper : IConfigureHowToFindSagaWithMessage
{
    public List<string> Properties  = new List<string>();
    public void ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty) where TSagaEntity : IContainSagaData
    {
        try
        {
            var extractProperty = ExtractProperty(sagaEntityProperty);
            Properties.Add(extractProperty);
        }
        catch (Exception exception)
        {
            var message = $"Could not configure mapping for {typeof (TSagaEntity)}. {exception.Message}.";
            throw new Exception(message);
        }
    }

    internal static string ExtractProperty<TSagaEntity>(Expression<Func<TSagaEntity, object>> expression)
    {
        var member = expression.Body as MemberExpression;
        if (member == null)
        {
            throw new Exception("Not a MemberExpression");
        }

        var property = member.Member as PropertyInfo;

        if (property == null)
        {
            throw new Exception("Not a Property Expression");
        }
        MappedTypeVerifier.Verify(property.PropertyType.FullName);
        return property.Name;
    }
}