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

        task = Task.Run(
            async () =>
            {
                try
                {
                    while (true)
                    {
                        token.ThrowIfCancellationRequested();

                        var utcNow = DateTime.UtcNow;

                        try
                        {
                            await delayStrategy(interval, token).ConfigureAwait(false);
                            await callback(utcNow, token).ConfigureAwait(false);
                        }
                        catch (Exception ex) when (!(ex is OperationCanceledException))
                        {
                            errorCallback(ex);
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        log.Debug("Timer execution canceled.", ex);
                    }
                    else
                    {
                        log.Warn("OperationCanceledException thrown.", ex);
                    }
                }
                catch (Exception ex)
                {
                    log.Debug("Timer execution failed.", ex);
                }
            },
            CancellationToken.None);
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
    readonly ILog log = LogManager.GetLogger<AsyncTimer>();
}
