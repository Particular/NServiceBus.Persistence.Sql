using System.Threading.Tasks;

public static class TaskEx
{
    public static void Await(this Task task)
    {
        task.GetAwaiter().GetResult();
    }
}