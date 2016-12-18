namespace NServiceBus.Persistence.Sql
{
    public class TimeoutCommands
    {
        public TimeoutCommands(string removeById, string next, string selectById, string add, string removeBySagaId, string range)
        {
            RemoveById = removeById;
            Next = next;
            SelectById = selectById;
            Add = add;
            RemoveBySagaId = removeBySagaId;
            Range = range;
        }
        public readonly string RemoveById;
        public readonly string Next;
        public readonly string SelectById;
        public readonly string Add;
        public readonly string RemoveBySagaId;
        public readonly string Range;
    }
}