using System;
using NServiceBus;

class Program
{
    static void Main()
    {
        var configuration = ConfigBuilder.Build("Saga");
        using (var bus = Bus.Create(configuration).Start())
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