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
        using (var bus = await Bus.Create(configuration).StartAsync())
        {
            Console.WriteLine("Press 'Enter' to publish a message");
            Console.WriteLine("Press any other key to exit");
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
                await bus.PublishAsync(myEvent);
            }
        }
    }
}