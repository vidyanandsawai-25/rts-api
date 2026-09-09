namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Queues work to run outside the HTTP request lifecycle ("fire-and-forget"). The enqueueing
/// caller must capture only plain data (ids, primitives) in the work item closure, never a
/// service resolved from the request's own DI scope -- that scope is disposed as soon as the
/// request completes. <see cref="QueueWorkItem"/>'s consumer (a hosted service) creates a
/// fresh scope and passes its <see cref="IServiceProvider"/> into the work item at execution time.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>Enqueues a work item. Returns immediately -- does not wait for the item to run.</summary>
    void QueueWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem);

    /// <summary>Dequeues the next work item, waiting if the queue is empty. Used by the processing hosted service only.</summary>
    Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}
