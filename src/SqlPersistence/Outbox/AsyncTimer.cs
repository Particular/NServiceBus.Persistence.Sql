using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Logging;

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
                catch (OperationCanceledException ex)
                {
                    // noop
                    if (token.IsCancellationRequested)
                    {
                        log.Debug("Timer execution cancelled.", ex);
                    }
                    else
                    {
                        log.Warn("OperationCanceledException thrown.", ex);
                    }
                }
                catch (Exception ex)
                {
                    errorCallback(ex);
                }
            }
        }, CancellationToken.None);
    }

    public Task Stop(CancellationToken cancellationToken = default)
    {
        if (tokenSource == null)
        {
            return Task.CompletedTask;
        }

        tokenSource.Cancel();
        tokenSource.Dispose();

        return task ?? Task.CompletedTask;
    }

    Task task;
    CancellationTokenSource tokenSource;
    ILog log = LogManager.GetLogger<AsyncTimer>();
}
