namespace Networker.Core;

public static class ParallelExecutor
{
    public static async Task RunAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        int maxConcurrency,
        Func<TItem, CancellationToken, Task<TResult>> work,
        IProgress<TResult>? progress,
        CancellationToken cancellationToken
        )
    {
        using var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task>();

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await semaphore.WaitAsync(cancellationToken);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await work(item, cancellationToken);
                    progress?.Report(result);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }
}
