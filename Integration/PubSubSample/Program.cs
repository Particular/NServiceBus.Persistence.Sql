using System;
using NServiceBus;

class Program
{
    static void Main()
    {
        var configuration = ConfigBuilder.Build("PubSub");
        using (var bus = Bus.Create(configuration).Start())
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
                bus.Publish(myEvent);
            }
            
        }
    }
}