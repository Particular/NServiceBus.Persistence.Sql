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
        var endpointConfiguration = ConfigBuilder.Build("Timeouts");
        var endpoint = await Endpoint.Start(endpointConfiguration);
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
                    var startSagaMessage = new StartSagaMessage
                    {
                        MySagaId = Guid.NewGuid()
                    };
                    await endpoint.SendLocal(startSagaMessage);
                    continue;
                }
                if (key.Key == ConsoleKey.D)
                {
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