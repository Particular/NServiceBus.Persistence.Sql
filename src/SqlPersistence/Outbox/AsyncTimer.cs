using System;
using System.Threading;
using System.Threading.Tasks;

class AsyncTimer : IAsyncTimer
{
    public void Start(Func<DateTime, CancellationToken, Task> callback, TimeSpan interval, Action<Exception> errorCallback, Func<TimeSpan, CancellationToken, Task> delayStrategy)
    {
        tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        task = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var utcNow = DateTime.UtcNow;
                    await delayStrategy(interval, token).ConfigureAwait(false);
                    await callback(utcNow, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // noop
                }
                catch (Exception ex)
                {
                    errorCallback(ex);
                }
            }
        }, CancellationToken.None);
    }

    public Task Stop()
    {
        if (tokenSource == null)
        {
            return Task.FromResult(0);
        }

        tokenSource.Cancel();
        tokenSource.Dispose();

        return task ?? Task.FromResult(0);
    }

    Task task;
    CancellationTokenSource tokenSource;
}