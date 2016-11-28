using System;
using NServiceBus.Extensibility;
using NServiceBus.Sagas;

static class ContextBagExtentions
{
    public static Type GetSagaType(this ContextBag context)
    {
        var activeSagaInstance = context.Get<ActiveSagaInstance>();
        if (activeSagaInstance != null)
        {
            return activeSagaInstance.Instance.GetType();
        }
        throw new Exception($"Expected to find an insatnce of {nameof(ActiveSagaInstance)} in the {nameof(ContextBag)}.");
    }
}