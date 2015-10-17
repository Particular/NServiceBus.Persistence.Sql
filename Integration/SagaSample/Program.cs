using System;
using NServiceBus;

class Program
{
    static void Main()
    {
        var configuration = ConfigBuilder.Build("Saga");
        using (var bus = Bus.Create(configuration).Start())
        {
            Console.WriteLine("Press 'Enter' to start a saga");
            Console.WriteLine("Press any other key to exit");
            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key != ConsoleKey.Enter)
                {
                    return;
                }
                bus.SendLocal(new StartSagaMessage
                {
                    MySagaId = "123"
                });
            }
            
        }
    }
}