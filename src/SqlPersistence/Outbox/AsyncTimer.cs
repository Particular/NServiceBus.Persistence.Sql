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
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var utcNow = DateTime.UtcNow;
                        await delayStrategy(interval, token).ConfigureAwait(false);
                        await callback(utcNow, token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex.IsCausedBy(token))
                    {
                        // private token, timer is being stopped, log the exception in case the stack trace is ever needed for debugging
                        log.Debug("Operation canceled while stopping timer.", ex);
                        break;
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            errorCallback(ex);
                        }
                        catch (Exception errorCallBackEx)
                        {
                            log.Error("Error call back failed. Stopping timer.", errorCallBackEx);
                            break;
                        }
                    }
                }
            },
            CancellationToken.None);
    }

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        tokenSource?.Cancel();

        await (task ?? Task.CompletedTask).ConfigureAwait(false);

        tokenSource?.Dispose();
    }

    Task task;
    CancellationTokenSource tokenSource;

    static readonly ILog log = LogManager.GetLogger<AsyncTimer>();
}
