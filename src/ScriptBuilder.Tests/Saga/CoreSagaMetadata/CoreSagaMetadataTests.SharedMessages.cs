using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class MessageA : ICommand
    {
        public string Correlation { get; set; }
    }

    public class MessageB : ICommand
    {
        public string Correlation { get; set; }
    }

    public class MessageC : ICommand
    {
        public string Part1 { get; set; }
        public string Part2 { get; set; }
    }

    public class MessageD : ICommand
    {
        public string DifferentName { get; set; }
    }
}
