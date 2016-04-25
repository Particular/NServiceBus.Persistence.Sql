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
        var endpoint = await Endpoint.Start(configuration);
        Console.WriteLine("Press 'S' to start a saga with a timeout");
        Console.WriteLine("Press 'D' to defer a message");
        Console.WriteLine("Press any other key to exit");
        try
        {
            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key == ConsoleKey.S)
                {
                    await endpoint.SendLocal(new StartSagaMessage());
                    continue;
                }
                if (key.Key == ConsoleKey.D)
                {
                    var deferMessage = new DeferMessage
                    {
                        Property = "PropertyValue"
                    };

                    var options = new SendOptions();

                    options.RouteToThisEndpoint();
                    options.DelayDeliveryWith(TimeSpan.FromSeconds(2));
                    await endpoint.Send(deferMessage, options);
                    continue;
                }
                return;
            }
        }
        finally
        {
            await endpoint.Stop();
        }
    }
}