using System;
using System.Threading;
using System.Threading.Tasks;

interface IAsyncTimer
{
    void Start(Func<DateTime, CancellationToken, Task> callback, TimeSpan interval, Action<Exception> errorCallback, Func<TimeSpan, CancellationToken, Task> delayStrategy);
    Task Stop(CancellationToken cancellationToken = default);
}