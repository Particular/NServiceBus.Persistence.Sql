using System;
using NServiceBus;

class Program
{
    static void Main()
    {
        var configuration = ConfigBuilder.Build("Timeouts");
        using (var bus = Bus.Create(configuration).Start())
        {
            Console.WriteLine("Press 'S' to start a saga with a timeout");
            Console.WriteLine("Press 'D' to defer a message");
            Console.WriteLine("Press any other key to exit");
            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key == ConsoleKey.S)
                {
                    bus.SendLocal(new StartSagaMessage());
                    continue;
                }
                if (key.Key == ConsoleKey.D)
                {
                    var deferMessage = new DeferMessage
                    {
                        Property = "PropertyValue"
                    };
                    bus.Defer(TimeSpan.FromSeconds(2), deferMessage);
                    continue;
                }
                return;
            }
        }
    }
}