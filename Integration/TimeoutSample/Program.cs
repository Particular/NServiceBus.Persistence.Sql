using System;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static void Main()
    {
        Start().GetAwaiter().GetResult();
    }

    static async Task Start()
    {
        var configuration = ConfigBuilder.Build("Timeouts");
        using (var bus = await Bus.Create(configuration).StartAsync())
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
                    await bus.SendLocalAsync(new StartSagaMessage());
                    continue;
                }
                if (key.Key == ConsoleKey.D)
                {
                    var deferMessage = new DeferMessage
                    {
                        Property = "PropertyValue"
                    };

                    var options = new SendOptions();

                    options.RouteToLocalEndpointInstance();
                    options.DelayDeliveryWith(TimeSpan.FromSeconds(2));
                    await bus.SendAsync(deferMessage, options);
                    continue;
                }
                return;
            }
        }
    }
}