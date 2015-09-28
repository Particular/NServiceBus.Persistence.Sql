using System;
using NServiceBus;

class Program
{
    static void Main()
    {
        var busConfiguration = ConfigBuilder.Build("Timeouts");
        using (var bus = Bus.Create(busConfiguration).Start())
        {
            var deferMessage = new DeferMessage
            {
                Property = "PropertyValue"
            };
            bus.Defer(TimeSpan.FromSeconds(2), deferMessage);
            bus.SendLocal(new StartSaga());
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}