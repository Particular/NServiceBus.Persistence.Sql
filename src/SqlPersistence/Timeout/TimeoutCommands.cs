namespace NServiceBus.Persistence.Sql
{
    public class TimeoutCommands
    {
        public TimeoutCommands(string removeById, string next, string peek, string add, string removeBySagaId, string range)
        {
            RemoveById = removeById;
            Next = next;
            Peek = peek;
            Add = add;
            RemoveBySagaId = removeBySagaId;
            Range = range;
        }
        public readonly string RemoveById;
        public readonly string Next;
        public readonly string Peek;
        public readonly string Add;
        public readonly string RemoveBySagaId;
        public readonly string Range;
    }
}