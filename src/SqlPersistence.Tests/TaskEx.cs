using System.Threading.Tasks;

public static class TaskEx
{
    public static void Await(this Task task) => task.GetAwaiter().GetResult();

#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    public static Task<T> ToTask<T>(this T target) => Task.FromResult(target);
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
}