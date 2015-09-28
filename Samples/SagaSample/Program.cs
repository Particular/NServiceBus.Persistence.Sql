using System;
using NServiceBus;

class Program
{
    static void Main()
    {
        var busConfiguration = ConfigBuilder.Build("Saga");
        using (var bus = Bus.Create(busConfiguration).Start())
        {
            bus.SendLocal(new StartSaga
            {
                MySagaId = "123"
            });
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}