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
        var configuration = ConfigBuilder.Build("Saga");
        var endpointInstance = await Endpoint.Start(configuration);
        var session = endpointInstance.CreateBusSession();
        Console.WriteLine("Press 'Enter' to start a saga");
        Console.WriteLine("Press any other key to exit");
        try
        {
            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key != ConsoleKey.Enter)
                {
                    return;
                }
                await session.SendLocal(new StartSagaMessage
                {
                    MySagaId = Guid.NewGuid()
                });
            }
        }
        finally
        {
            await endpointInstance.Stop();
        }

    }
}