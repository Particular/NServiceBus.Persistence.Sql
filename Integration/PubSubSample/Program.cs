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
        var configuration = ConfigBuilder.Build("PubSub");
        var endpointInstance = await Endpoint.Start(configuration);
        var session = endpointInstance.CreateBusSession();
        Console.WriteLine("Press 'Enter' to publish a message");
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
                var myEvent = new MyEvent
                {
                    Property = "PropertyValue"
                };
                await session.Publish(myEvent);
            }
        }
        finally
        {
            await endpointInstance.Stop();
        }
    }
}