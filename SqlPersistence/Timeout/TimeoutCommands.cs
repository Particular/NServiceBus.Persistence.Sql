namespace NServiceBus.Persistence.Sql
{
    public class TimeoutCommands
    {
        public TimeoutCommands(string removeById, string next, string selectById, string insert, string removeBySagaId, string range)
        {
            RemoveById = removeById;
            Next = next;
            SelectById = selectById;
            Insert = insert;
            RemoveBySagaId = removeBySagaId;
            Range = range;
        }
        public readonly string RemoveById;
        public readonly string Next;
        public readonly string SelectById;
        public readonly string Insert;
        public readonly string RemoveBySagaId;
        public readonly string Range;
    }
}